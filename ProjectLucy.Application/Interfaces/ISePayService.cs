using ProjectLucy.Application.DTOs.PaymentDtos;

namespace ProjectLucy.Application.Interfaces;

/// <summary>
/// SePay integration contract. Implemented in Infrastructure (HMAC-SHA256,
/// signature rules). Kept in Application so handlers depend on the abstraction.
/// </summary>
public interface ISePayService
{
    /// <summary>Build the form payload that the frontend submits to the gateway.</summary>
    SePayFormData BuildFormData(CreatePaymentRequest request);

    /// <summary>Compute the HMAC-SHA256 signature over an arbitrary field map.</summary>
    string CreateSignature(IDictionary<string, string> fields);

    /// <summary>Verify a signature coming back from a SePay IPN webhook.</summary>
    bool VerifyIpnSignature(IDictionary<string, string> fields, string providedSignature);
}
