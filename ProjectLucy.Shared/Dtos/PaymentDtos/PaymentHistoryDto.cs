namespace ProjectLucy.Shared.Dtos.PaymentDtos;

/// <summary>
/// A single payment transaction shown in the user's payment history.
/// </summary>
public class PaymentHistoryDto
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }

    /// <summary>Gateway invoice number (reference_code).</summary>
    public string? ReferenceCode { get; set; }

    public string? Description { get; set; }

    /// <summary>transaction_type name (e.g. SePay Online Payment).</summary>
    public string? PaymentType { get; set; }

    public DateTime? CreatedAt { get; set; }
}
