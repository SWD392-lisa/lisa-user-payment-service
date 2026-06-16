using System.Text.Json.Serialization;

namespace ProjectLucy.Shared.Dtos.PaymentDtos;

/// <summary>
/// Pre-built form payload that the frontend will submit (as a hidden HTML form
/// or as JSON to its own /api/payment/checkout proxy) to the SePay gateway.
/// </summary>
public class SePayFormData
{
    [JsonPropertyName("orderAmount")]
    public string OrderAmount { get; set; } = string.Empty;
    [JsonPropertyName("merchant")]
    public string Merchant { get; set; } = string.Empty;
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = "PURCHASE";
    [JsonPropertyName("orderDescription")]
    public string OrderDescription { get; set; } = string.Empty;
    [JsonPropertyName("orderInvoiceNumber")]
    public string OrderInvoiceNumber { get; set; } = string.Empty;
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }
    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }
    [JsonPropertyName("successUrl")]
    public string SuccessUrl { get; set; } = string.Empty;
    [JsonPropertyName("errorUrl")]
    public string ErrorUrl { get; set; } = string.Empty;
    [JsonPropertyName("cancelUrl")]
    public string CancelUrl { get; set; } = string.Empty;
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
    [JsonPropertyName("isSandbox")]
    public bool IsSandbox { get; set; }
}
