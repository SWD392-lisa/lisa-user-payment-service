using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.Payment.Commands.CreatePayment;

public record CreatePaymentCommand(CreatePaymentRequest Request, Guid UserId)
    : IRequest<Result<SePayFormData>>;
