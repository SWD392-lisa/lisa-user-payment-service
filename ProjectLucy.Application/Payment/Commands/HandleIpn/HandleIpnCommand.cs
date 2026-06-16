using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Shared.Dtos.PaymentDtos;

namespace ProjectLucy.Application.Payment.Commands.HandleIpn;

public record HandleIpnCommand(IpnRequest Request) : IRequest<Result<object>>;
