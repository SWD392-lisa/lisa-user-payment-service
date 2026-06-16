using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLucy.Shared.Dtos.PaymentDtos;

public class CreatePaymentRequest
{
    [JsonPropertyName("orderInvoiceNumber")]
    [Required(ErrorMessage = "order_invoice_number is required")]
    [MaxLength(100)]
    public string OrderInvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("orderAmount")]
    [Range(1, long.MaxValue, ErrorMessage = "order_amount must be greater than 0")]
    public long OrderAmount { get; set; }

    [JsonPropertyName("orderDescription")]
    [Required(ErrorMessage = "order_description is required")]
    [MaxLength(255)]
    public string OrderDescription { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    [MaxLength(100)]
    public string? CustomerId { get; set; }

    [JsonPropertyName("paymentMethod")]
    /// <summary>Optional. One of CARD, BANK_TRANSFER, NAPAS_BANK_TRANSFER.</summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
}
