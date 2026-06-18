using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;
using ProjectLucy.Application.DTOs.LoginDtos.Childs;

namespace ProjectLucy.Application.RoleUpgrade.Commands.UpgradeUsingWallet;

public class UpgradeUsingWalletCommandHandler : IRequestHandler<UpgradeUsingWalletCommand, Result<UpgradeUsingWalletResponse>>
{
    private readonly IUserRepository _userRepo;
    private readonly IRolePriceRepository _rolePriceRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IRoleUpgradeOrderRepository _upgradeOrderRepo;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    private const string TransactionTypeCode = "ROLE_UPGRADE_WALLET";

    public UpgradeUsingWalletCommandHandler(
        IUserRepository userRepo,
        IRolePriceRepository rolePriceRepo,
        IWalletRepository walletRepo,
        ITransactionRepository transactionRepo,
        IRoleUpgradeOrderRepository upgradeOrderRepo,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _rolePriceRepo = rolePriceRepo;
        _walletRepo = walletRepo;
        _transactionRepo = transactionRepo;
        _upgradeOrderRepo = upgradeOrderRepo;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UpgradeUsingWalletResponse>> Handle(UpgradeUsingWalletCommand cmd, CancellationToken ct)
    {
        // 1. Load the user (tracked for update)
        var user = await _userRepo.GetByIdTrackedAsync(cmd.UserId, ct);
        if (user is null)
            return Result<UpgradeUsingWalletResponse>.Failure(404, "User not found");

        // 2. Load the role price
        var rolePrice = await _rolePriceRepo.GetByIdAsync(cmd.Request.RolePriceId, ct);
        if (rolePrice is null)
            return Result<UpgradeUsingWalletResponse>.Failure(404, "Role price not found");

        if (rolePrice.IsActive != true)
            return Result<UpgradeUsingWalletResponse>.Failure(400, "This upgrade package is no longer available");

        // 3. Validate user doesn't already have this role
        if (rolePrice.RoleId == user.RoleId)
            return Result<UpgradeUsingWalletResponse>.Failure(400, "You already have this role");

        // 4. Load wallet & check balance
        var wallet = await _walletRepo.GetByUserIdAsync(cmd.UserId, ct);
        if (wallet is null)
            return Result<UpgradeUsingWalletResponse>.Failure(400, "Wallet not found");

        if (wallet.Balance < rolePrice.Price)
            return Result<UpgradeUsingWalletResponse>.Failure(402, "Insufficient wallet balance");

        // 5. Resolve transaction type
        var typeId = await _transactionRepo.GetTypeIdByCodeAsync(TransactionTypeCode, ct);
        if (typeId is null)
            throw new ServerException($"Transaction type '{TransactionTypeCode}' not found.");

        // 6. Create transaction
        var transaction = new Transaction
        {
            TransactionTypeId = typeId.Value,
            UserId = cmd.UserId,
            Amount = rolePrice.Price,
            Currency = rolePrice.Currency ?? "VND",
            TransactionContent = $"Nâng cấp tài khoản lên {rolePrice.Role.RoleName}",
            Status = "completed",
            ReferenceCode = $"UPG_WALLET_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _transactionRepo.AddAsync(transaction, ct);

        // 7. Deduct from wallet
        wallet.Balance -= rolePrice.Price;
        wallet.UpdatedAt = DateTime.UtcNow;

        // 8. Create wallet ledger
        var ledgerEntry = new WalletLedger
        {
            WalletId = wallet.Id,
            Transaction = transaction,
            Amount = -rolePrice.Price,
            BalanceAfter = wallet.Balance,
            EntryType = "ROLE_UPGRADE",
            Note = $"Nâng cấp {rolePrice.Role.RoleName} qua ví",
            CreatedAt = DateTime.UtcNow
        };
        wallet.WalletLedgers.Add(ledgerEntry);

        // 9. Create role upgrade order (activated immediately)
        var upgradeOrder = new RoleUpgradeOrder
        {
            Id = Guid.NewGuid(),
            Transaction = transaction,
            UserId = cmd.UserId,
            FromRoleId = user.RoleId,
            ToRoleId = rolePrice.RoleId,
            RolePriceId = rolePrice.Id,
            ActivatedAt = DateTime.UtcNow,
            ExpiresAt = rolePrice.Duration.HasValue
                ? DateTime.UtcNow.Add(rolePrice.Duration.Value)
                : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _upgradeOrderRepo.AddAsync(upgradeOrder, ct);

        // 10. Update user's role
        var previousRoleId = user.RoleId;
        user.RoleId = rolePrice.RoleId;
        user.UpdatedAt = DateTime.UtcNow;

        // 11. Generate new JWT with updated role
        var newToken = _jwtTokenService.GenerateAccessToken(user);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<UpgradeUsingWalletResponse>.Success(
            new UpgradeUsingWalletResponse
            {
                NewAccessToken = newToken,
                User = new UserInfoDto
                {
                    UserId = user.UserId,
                    FullName = user.UserFullName,
                    Email = user.UserEmail,
                    RoleCode = rolePrice.Role.RoleCode,
                    RoleName = rolePrice.Role.RoleName
                }
            },
            $"Nâng cấp lên {rolePrice.Role.RoleName} thành công");
    }
}
