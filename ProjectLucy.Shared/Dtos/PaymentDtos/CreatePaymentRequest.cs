using System.ComponentModel.DataAnnotations;

namespace ProjectLucy.Shared.Dtos.PaymentDtos;

public class CreatePaymentRequest
{
    [Required(ErrorMessage = "order_invoice_number is required")]
    [MaxLength(100)]
    public string OrderInvoiceNumber { get; set; } = string.Empty;

    [Range(1, long.MaxValue, ErrorMessage = "order_amount must be greater than 0")]
    public long OrderAmount { get; set; }

    [Required(ErrorMessage = "order_description is required")]
    [MaxLength(255)]
    public string OrderDescription { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CustomerId { get; set; }

    /// <summary>Optional. One of CARD, BANK_TRANSFER, NAPAS_BANK_TRANSFER.</summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
}
