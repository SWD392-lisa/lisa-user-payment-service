using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Shared.Dtos.WalletDtos;

namespace ProjectLucy.Application.Wallet.Queries.GetWalletBalance;

public class GetWalletBalanceQueryHandler
    : IRequestHandler<GetWalletBalanceQuery, Result<WalletBalanceDto>>
{
    private readonly IWalletRepository _walletRepo;

    public GetWalletBalanceQueryHandler(IWalletRepository walletRepo)
    {
        _walletRepo = walletRepo;
    }

    public async Task<Result<WalletBalanceDto>> Handle(
        GetWalletBalanceQuery query, CancellationToken ct)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(query.UserId, ct);

        if (wallet is null)
            return Result<WalletBalanceDto>.Success(
                new WalletBalanceDto { Balance = 0, Currency = "VND" },
                "Wallet not yet created — balance is zero");

        return Result<WalletBalanceDto>.Success(
            new WalletBalanceDto
            {
                Balance = wallet.Balance,
                Currency = wallet.Currency
            },
            "Wallet balance retrieved");
    }
}
