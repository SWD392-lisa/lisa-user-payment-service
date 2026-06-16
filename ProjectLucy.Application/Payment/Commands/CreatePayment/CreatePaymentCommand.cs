using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Shared.Dtos.PaymentDtos;

namespace ProjectLucy.Application.Payment.Commands.CreatePayment;

public record CreatePaymentCommand(CreatePaymentRequest Request, Guid UserId)
    : IRequest<Result<SePayFormData>>;
