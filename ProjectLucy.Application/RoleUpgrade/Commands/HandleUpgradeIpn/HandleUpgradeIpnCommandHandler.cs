using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.RoleUpgrade.Commands.HandleUpgradeIpn;

public class HandleUpgradeIpnCommandHandler : IRequestHandler<HandleUpgradeIpnCommand, Result<object>>
{
    private readonly ISePayService _sePayService;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IRoleUpgradeOrderRepository _upgradeOrderRepo;
    private readonly IRolePriceRepository _rolePriceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string[] TerminalStatuses = ["completed", "failed", "cancelled"];

    public HandleUpgradeIpnCommandHandler(
        ISePayService sePayService,
        ITransactionRepository transactionRepo,
        IRoleUpgradeOrderRepository upgradeOrderRepo,
        IRolePriceRepository rolePriceRepo,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork)
    {
        _sePayService = sePayService;
        _transactionRepo = transactionRepo;
        _upgradeOrderRepo = upgradeOrderRepo;
        _rolePriceRepo = rolePriceRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<object>> Handle(HandleUpgradeIpnCommand cmd, CancellationToken ct)
    {
        var ipn = cmd.Request;

        // 1. Verify signature
        var fields = new Dictionary<string, string>
        {
            ["order_invoice_number"] = ipn.OrderInvoiceNumber ?? string.Empty,
            ["transaction_status"]   = ipn.TransactionStatus ?? string.Empty,
            ["transaction_id"]       = ipn.TransactionId ?? string.Empty,
            ["order_amount"]         = ipn.OrderAmount ?? string.Empty,
            ["payment_method"]       = ipn.PaymentMethod ?? string.Empty,
        };

        if (!_sePayService.VerifyIpnSignature(fields, ipn.Signature ?? string.Empty))
            throw new ForbiddenException("Invalid IPN signature");

        // 2. Resolve transaction
        var transaction = await _transactionRepo.GetByReferenceCodeAsync(ipn.OrderInvoiceNumber!, ct);
        if (transaction is null)
            throw new NotFoundException("Transaction not found");

        // 3. Idempotency
        var currentStatus = (transaction.Status ?? string.Empty).ToLowerInvariant();
        if (TerminalStatuses.Contains(currentStatus))
            return Result<object>.Success(new { received = true }, "IPN already processed");

        // 4. Map status
        var newStatus = ipn.TransactionStatus?.ToLowerInvariant() switch
        {
            "success"   => "completed",
            "failed"    => "failed",
            "cancelled" => "cancelled",
            _           => transaction.Status
        };
        transaction.Status = newStatus;
        transaction.UpdatedAt = DateTime.UtcNow;

        // 5. Process upgrade on success
        if (newStatus == "completed")
        {
            var upgradeOrder = await _upgradeOrderRepo.GetByTransactionIdTrackedAsync(transaction.Id, ct);
            if (upgradeOrder is null)
                throw new NotFoundException("Upgrade order not found for this transaction");

            upgradeOrder.ActivatedAt = DateTime.UtcNow;

            var rolePrice = await _rolePriceRepo.GetByIdAsync(upgradeOrder.RolePriceId, ct);
            if (rolePrice?.Duration.HasValue == true)
                upgradeOrder.ExpiresAt = DateTime.UtcNow.Add(rolePrice.Duration.Value);

            upgradeOrder.UpdatedAt = DateTime.UtcNow;

            var user = await _userRepo.GetByIdTrackedAsync(transaction.UserId, ct);
            if (user is null)
                throw new NotFoundException("User not found");

            user.RoleId = upgradeOrder.ToRoleId;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<object>.Success(new { received = true }, "Upgrade IPN processed");
    }
}
