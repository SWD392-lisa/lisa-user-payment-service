using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.RoleUpgrade.Commands.CreateUpgradePayment;

public record CreateUpgradePaymentCommand(CreateUpgradePaymentRequest Request, Guid UserId)
    : IRequest<Result<SePayFormData>>;
