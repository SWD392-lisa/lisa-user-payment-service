using System.Text.Json.Serialization;

namespace ProjectLucy.Application.DTOs.RoleUpgradeDtos;

public class UpgradeWalletRequest
{
    [JsonPropertyName("rolePriceId")]
    public int RolePriceId { get; set; }
}
