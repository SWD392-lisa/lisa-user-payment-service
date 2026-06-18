using MediatR;
using Microsoft.Extensions.Logging;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;
using ProjectLucy.Application.DTOs.LoginDtos.Childs;

namespace ProjectLucy.Application.RoleUpgrade.Commands.ConfirmUpgradePayment;

public class ConfirmUpgradePaymentCommandHandler : IRequestHandler<ConfirmUpgradePaymentCommand, Result<ConfirmUpgradePaymentResponse>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IRoleUpgradeOrderRepository _upgradeOrderRepo;
    private readonly IRolePriceRepository _rolePriceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmUpgradePaymentCommandHandler> _logger;

    private static readonly string[] TerminalStatuses = ["completed", "failed", "cancelled"];

    public ConfirmUpgradePaymentCommandHandler(
        ITransactionRepository transactionRepo,
        IRoleUpgradeOrderRepository upgradeOrderRepo,
        IRolePriceRepository rolePriceRepo,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmUpgradePaymentCommandHandler> logger)
    {
        _transactionRepo = transactionRepo;
        _upgradeOrderRepo = upgradeOrderRepo;
        _rolePriceRepo = rolePriceRepo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ConfirmUpgradePaymentResponse>> Handle(ConfirmUpgradePaymentCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        _logger.LogInformation(
            "ConfirmUpgradePayment START — UserId={UserId}, Invoice={Invoice}, Status={Status}",
            cmd.UserId, req.OrderInvoiceNumber, req.Status);

        // 1. Look up the pending transaction
        var transaction = await _transactionRepo.GetByReferenceCodeAsync(req.OrderInvoiceNumber, ct);
        if (transaction is null)
        {
            _logger.LogWarning("ConfirmUpgradePayment FAIL — Transaction not found for Invoice={Invoice}", req.OrderInvoiceNumber);
            return Result<ConfirmUpgradePaymentResponse>.Failure(404, "Transaction not found");
        }

        // 2. Verify ownership
        if (transaction.UserId != cmd.UserId)
        {
            _logger.LogWarning("ConfirmUpgradePayment FAIL — UserId mismatch");
            return Result<ConfirmUpgradePaymentResponse>.Failure(403, "This transaction does not belong to the current user");
        }

        // 3. Idempotency check
        var currentStatus = (transaction.Status ?? string.Empty).ToLowerInvariant();
        if (TerminalStatuses.Contains(currentStatus))
        {
            _logger.LogInformation("ConfirmUpgradePayment IDEMPOTENT — already {Status}", currentStatus);
            return Result<ConfirmUpgradePaymentResponse>.Success(
                await BuildResponseForExistingUpgrade(transaction.Id, cmd.UserId, ct),
                "Upgrade already processed");
        }

        // 4. Map status
        var newStatus = req.Status.ToLowerInvariant() switch
        {
            "success" => "completed",
            "failed" => "failed",
            "cancelled" => "cancelled",
            _ => transaction.Status
        };
        transaction.Status = newStatus;
        transaction.UpdatedAt = DateTime.UtcNow;

        ConfirmUpgradePaymentResponse response;

        // 5. Process upgrade on successful payment
        if (newStatus == "completed")
        {
            response = await ProcessUpgrade(transaction, cmd.UserId, ct);
        }
        else
        {
            // Payment failed or cancelled — just update transaction status
            _logger.LogInformation("ConfirmUpgradePayment — Payment {Status}, no upgrade processed", newStatus);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<ConfirmUpgradePaymentResponse>.Failure(400, $"Payment {newStatus}, upgrade not processed");
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("ConfirmUpgradePayment SUCCESS — Invoice={Invoice}", req.OrderInvoiceNumber);
        return Result<ConfirmUpgradePaymentResponse>.Success(response, "Nâng cấp tài khoản thành công");
    }

    private async Task<ConfirmUpgradePaymentResponse> ProcessUpgrade(Transaction transaction, Guid userId, CancellationToken ct)
    {
        // 1. Load the associated upgrade order
        var upgradeOrder = await _upgradeOrderRepo.GetByTransactionIdTrackedAsync(transaction.Id, ct);
        if (upgradeOrder is null)
        {
            _logger.LogError("No RoleUpgradeOrder found for transaction {TxnId}", transaction.Id);
            throw new InvalidOperationException("Upgrade order not found for this transaction");
        }

        // 2. Activate the upgrade order
        upgradeOrder.ActivatedAt = DateTime.UtcNow;

        // 3. Set expires_at from role price duration
        var rolePrice = await _rolePriceRepo.GetByIdAsync(upgradeOrder.RolePriceId, ct);
        if (rolePrice?.Duration.HasValue == true)
        {
            upgradeOrder.ExpiresAt = DateTime.UtcNow.Add(rolePrice.Duration.Value);
        }
        upgradeOrder.UpdatedAt = DateTime.UtcNow;

        // 4. Load user (tracked) and update role
        var user = await _userRepo.GetByIdTrackedAsync(userId, ct);
        if (user is null)
            throw new InvalidOperationException("User not found");

        user.RoleId = upgradeOrder.ToRoleId;
        user.UpdatedAt = DateTime.UtcNow;

        // 4. Generate new JWT with updated role
        var newToken = _jwtTokenService.GenerateAccessToken(user);

        // 5. Load role info for response
        var role = await _roleRepo.GetByIdAsync(upgradeOrder.ToRoleId, ct);

        return new ConfirmUpgradePaymentResponse
        {
            NewAccessToken = newToken,
            User = new UserInfoDto
            {
                UserId = user.UserId,
                FullName = user.UserFullName,
                Email = user.UserEmail,
                RoleCode = role?.RoleCode ?? "",
                RoleName = role?.RoleName ?? ""
            }
        };
    }

    private async Task<ConfirmUpgradePaymentResponse> BuildResponseForExistingUpgrade(long transactionId, Guid userId, CancellationToken ct)
    {
        // If already processed, just return current state
        var user = await _userRepo.GetByIdAsync(userId, ct);
        var role = user is not null ? await _roleRepo.GetByIdAsync(user.RoleId, ct) : null;

        return new ConfirmUpgradePaymentResponse
        {
            NewAccessToken = "",
            User = new UserInfoDto
            {
                UserId = user?.UserId ?? userId,
                FullName = user?.UserFullName ?? "",
                Email = user?.UserEmail ?? "",
                RoleCode = role?.RoleCode ?? "",
                RoleName = role?.RoleName ?? ""
            }
        };
    }
}
