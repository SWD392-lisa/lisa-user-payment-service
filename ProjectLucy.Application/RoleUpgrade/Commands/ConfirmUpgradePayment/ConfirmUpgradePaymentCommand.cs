using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.ConfirmPaymentDtos;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;

namespace ProjectLucy.Application.RoleUpgrade.Commands.ConfirmUpgradePayment;

public record ConfirmUpgradePaymentCommand(ConfirmPaymentRequest Request, Guid UserId)
    : IRequest<Result<ConfirmUpgradePaymentResponse>>;
