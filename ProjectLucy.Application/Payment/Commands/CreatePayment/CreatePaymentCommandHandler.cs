using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.Payment.Commands.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Result<SePayFormData>>
{
    private readonly ISePayService _sePayService;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>transaction_type.code used for SePay online payments.</summary>
    public const string SePayTransactionTypeCode = "ONLINE_SEPAY";

    public CreatePaymentCommandHandler(
        ISePayService sePayService,
        ITransactionRepository transactionRepo,
        IUnitOfWork unitOfWork)
    {
        _sePayService = sePayService;
        _transactionRepo = transactionRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SePayFormData>> Handle(CreatePaymentCommand cmd, CancellationToken ct)
    {
        // 1. Resolve the SePay transaction type (must be seeded in transaction_type)
        var typeId = await _transactionRepo.GetTypeIdByCodeAsync(SePayTransactionTypeCode, ct);
        if (typeId is null)
            throw new ServerException(
                $"Transaction type '{SePayTransactionTypeCode}' not found. Please seed transaction types first.");

        // 2. Persist a PENDING transaction so the IPN handler can resolve it later.
        //    reference_code holds the unique gateway invoice number.
        var transaction = new Transaction
        {
            TransactionTypeId = typeId.Value,
            UserId = cmd.UserId,
            Amount = cmd.Request.OrderAmount,
            Currency = "VND",
            TransactionContent = cmd.Request.OrderDescription,
            Status = "pending",
            ReferenceCode = cmd.Request.OrderInvoiceNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _transactionRepo.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 3. Build signed form data with the callback URLs including transaction info
        var formData = _sePayService.BuildFormData(cmd.Request, transaction.Id);

        return Result<SePayFormData>.Success(formData, "Payment form data created");
    }
}
