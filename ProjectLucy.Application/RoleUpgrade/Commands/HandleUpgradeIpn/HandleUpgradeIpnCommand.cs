using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.RoleUpgrade.Commands.HandleUpgradeIpn;

public record HandleUpgradeIpnCommand(IpnRequest Request) : IRequest<Result<object>>;
