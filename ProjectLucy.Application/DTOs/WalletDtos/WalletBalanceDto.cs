namespace ProjectLucy.Application.DTOs.WalletDtos;

public class WalletBalanceDto
{
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "VND";
}
