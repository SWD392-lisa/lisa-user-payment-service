using System.Text.Json.Serialization;
using ProjectLucy.Application.DTOs.LoginDtos.Childs;

namespace ProjectLucy.Application.DTOs.RoleUpgradeDtos;

public class UpgradeUsingWalletResponse
{
    [JsonPropertyName("newAccessToken")]
    public string NewAccessToken { get; set; } = null!;

    [JsonPropertyName("user")]
    public UserInfoDto User { get; set; } = null!;
}
