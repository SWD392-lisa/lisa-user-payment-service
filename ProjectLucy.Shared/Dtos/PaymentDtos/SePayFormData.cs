namespace ProjectLucy.Shared.Dtos.PaymentDtos;

/// <summary>
/// Pre-built form payload that the frontend will submit (as a hidden HTML form
/// or as JSON to its own /api/payment/checkout proxy) to the SePay gateway.
/// </summary>
public class SePayFormData
{
    public string OrderAmount { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public string Operation { get; set; } = "PURCHASE";
    public string OrderDescription { get; set; } = string.Empty;
    public string OrderInvoiceNumber { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? PaymentMethod { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string ErrorUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public bool IsSandbox { get; set; }
}
