using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.Payment.Queries.GetPaymentHistory;

public class GetPaymentHistoryQueryHandler
    : IRequestHandler<GetPaymentHistoryQuery, Result<IReadOnlyList<PaymentHistoryDto>>>
{
    private readonly ITransactionRepository _transactionRepo;

    public GetPaymentHistoryQueryHandler(ITransactionRepository transactionRepo)
    {
        _transactionRepo = transactionRepo;
    }

    public async Task<Result<IReadOnlyList<PaymentHistoryDto>>> Handle(
        GetPaymentHistoryQuery query, CancellationToken ct)
    {
        var transactions = await _transactionRepo.GetByUserAsync(query.UserId, ct);

        var history = transactions
            .Select(t => new PaymentHistoryDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Currency = t.Currency,
                Status = t.Status,
                ReferenceCode = t.ReferenceCode,
                Description = t.TransactionContent,
                PaymentType = t.TransactionType?.Name,
                CreatedAt = t.CreatedAt
            })
            .ToList();

        return Result<IReadOnlyList<PaymentHistoryDto>>.Success(history, "Payment history retrieved");
    }
}
