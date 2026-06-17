using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Shared.Dtos.WalletDtos;

namespace ProjectLucy.Application.Wallet.Queries.GetWalletBalance;

public record GetWalletBalanceQuery(Guid UserId)
    : IRequest<Result<WalletBalanceDto>>;
