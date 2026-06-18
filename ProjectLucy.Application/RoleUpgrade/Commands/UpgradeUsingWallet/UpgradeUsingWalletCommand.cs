using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;

namespace ProjectLucy.Application.RoleUpgrade.Commands.UpgradeUsingWallet;

public record UpgradeUsingWalletCommand(UpgradeWalletRequest Request, Guid UserId)
    : IRequest<Result<UpgradeUsingWalletResponse>>;
