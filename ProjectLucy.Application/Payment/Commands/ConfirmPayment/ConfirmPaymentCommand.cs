using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.ConfirmPaymentDtos;

namespace ProjectLucy.Application.Payment.Commands.ConfirmPayment;

public record ConfirmPaymentCommand(ConfirmPaymentRequest Request, Guid UserId)
    : IRequest<Result<object>>;
