namespace ProjectLucy.Application.DTOs.ConfirmPaymentDtos;

/// <summary>
/// Request from the frontend after SePay redirects the user back.
/// The frontend reads these values from the URL query parameters SePay appends.
/// </summary>
public class ConfirmPaymentRequest
{
    public string OrderInvoiceNumber { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Status { get; set; } = "success";
}
