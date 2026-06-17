using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLucy.Application.DTOs.PaymentDtos;

/// <summary>
/// Webhook payload SePay posts to /api/payment/ipn.
/// SePay sends the body in snake_case, so each property is mapped explicitly
/// via [JsonPropertyName] to keep binding independent of the API's casing policy.
/// </summary>
public class IpnRequest
{
    [Required]
    [JsonPropertyName("order_invoice_number")]
    public string? OrderInvoiceNumber { get; set; }

    [Required]
    [JsonPropertyName("transaction_status")]
    public string? TransactionStatus { get; set; }

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("order_amount")]
    public string? OrderAmount { get; set; }

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    [Required]
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}
