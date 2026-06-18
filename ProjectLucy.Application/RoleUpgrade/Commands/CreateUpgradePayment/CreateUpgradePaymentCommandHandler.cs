using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.RoleUpgrade.Commands.CreateUpgradePayment;

public class CreateUpgradePaymentCommandHandler : IRequestHandler<CreateUpgradePaymentCommand, Result<SePayFormData>>
{
    private readonly IUserRepository _userRepo;
    private readonly IRolePriceRepository _rolePriceRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IRoleUpgradeOrderRepository _upgradeOrderRepo;
    private readonly ISePayService _sePayService;
    private readonly IUnitOfWork _unitOfWork;

    private const string TransactionTypeCode = "ROLE_UPGRADE_SEPAY";

    public CreateUpgradePaymentCommandHandler(
        IUserRepository userRepo,
        IRolePriceRepository rolePriceRepo,
        ITransactionRepository transactionRepo,
        IRoleUpgradeOrderRepository upgradeOrderRepo,
        ISePayService sePayService,
        IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _rolePriceRepo = rolePriceRepo;
        _transactionRepo = transactionRepo;
        _upgradeOrderRepo = upgradeOrderRepo;
        _sePayService = sePayService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SePayFormData>> Handle(CreateUpgradePaymentCommand cmd, CancellationToken ct)
    {
        // 1. Validate user
        var user = await _userRepo.GetByIdAsync(cmd.UserId, ct);
        if (user is null)
            return Result<SePayFormData>.Failure(404, "User not found");

        // 2. Validate role price
        var rolePrice = await _rolePriceRepo.GetByIdAsync(cmd.Request.RolePriceId, ct);
        if (rolePrice is null)
            return Result<SePayFormData>.Failure(404, "Role price not found");

        if (rolePrice.IsActive != true)
            return Result<SePayFormData>.Failure(400, "This upgrade package is no longer available");

        if (rolePrice.RoleId == user.RoleId)
            return Result<SePayFormData>.Failure(400, "You already have this role");

        // 3. Resolve transaction type
        var typeId = await _transactionRepo.GetTypeIdByCodeAsync(TransactionTypeCode, ct);
        if (typeId is null)
            throw new ServerException($"Transaction type '{TransactionTypeCode}' not found.");

        // 4. Generate invoice number
        var invoiceNumber = $"UPG_SEPAY_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
        if (invoiceNumber.Length > 100)
            invoiceNumber = invoiceNumber[..100];

        // 5. Create pending transaction
        var transaction = new Transaction
        {
            TransactionTypeId = typeId.Value,
            UserId = cmd.UserId,
            Amount = rolePrice.Price,
            Currency = rolePrice.Currency ?? "VND",
            TransactionContent = $"Nâng cấp tài khoản lên {rolePrice.Role.RoleName}",
            Status = "pending",
            ReferenceCode = invoiceNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _transactionRepo.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 6. Create role upgrade order (not activated yet — will activate on payment confirm)
        var upgradeOrder = new RoleUpgradeOrder
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            UserId = cmd.UserId,
            FromRoleId = user.RoleId,
            ToRoleId = rolePrice.RoleId,
            RolePriceId = rolePrice.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _upgradeOrderRepo.AddAsync(upgradeOrder, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 7. Build SePay form data with upgrade-specific callback URLs
        var sePayRequest = new CreatePaymentRequest
        {
            OrderInvoiceNumber = invoiceNumber,
            OrderAmount = (long)rolePrice.Price,
            OrderDescription = $"Nâng cấp {rolePrice.Role.RoleName}",
            CustomerId = cmd.UserId.ToString(),
            PaymentMethod = cmd.Request.PaymentMethod
        };

        var formData = _sePayService.BuildFormData(sePayRequest, transaction.Id, new Dictionary<string, string>
        {
            ["type"] = "upgrade"
        });

        return Result<SePayFormData>.Success(formData, "Upgrade payment form data created");
    }
}
