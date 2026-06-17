using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.Payment.Commands.HandleIpn;

public record HandleIpnCommand(IpnRequest Request) : IRequest<Result<object>>;
