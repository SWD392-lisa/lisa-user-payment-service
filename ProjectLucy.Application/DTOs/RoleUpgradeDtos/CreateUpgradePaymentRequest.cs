using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLucy.Application.DTOs.RoleUpgradeDtos;

public class CreateUpgradePaymentRequest
{
    [JsonPropertyName("rolePriceId")]
    public int RolePriceId { get; set; }

    [JsonPropertyName("paymentMethod")]
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
}
