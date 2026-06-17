using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.Payment.Queries.GetPaymentHistory;

public record GetPaymentHistoryQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<PaymentHistoryDto>>>;
