using System.Text.Json.Serialization;

namespace ProjectLucy.Application.DTOs.RoleUpgradeDtos;

public class RolePriceDto
{
    [JsonPropertyName("rolePriceId")]
    public int RolePriceId { get; set; }

    [JsonPropertyName("roleCode")]
    public string RoleCode { get; set; } = null!;

    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = null!;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isCurrentRole")]
    public bool IsCurrentRole { get; set; }
}
