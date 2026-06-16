using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Application.Settings;
using ProjectLucy.Shared.Dtos.PaymentDtos;

namespace ProjectLucy.Infrastructure.Payment;

public class SePayService : ISePayService
{
    private readonly SePayOptions _options;

    /// <summary>
    /// Field order MUST match the gateway spec — do not reorder.
    /// Empty / missing values are filtered out before signing.
    /// </summary>
    private static readonly string[] AllowedCheckoutFields =
    [
        "order_amount", "merchant", "currency", "operation",
        "order_description", "order_invoice_number", "customer_id",
        "payment_method", "success_url", "error_url", "cancel_url",
    ];

    /// <summary>
    /// Field order for IPN signature verification. Same rule — do not reorder.
    /// </summary>
    private static readonly string[] AllowedIpnFields =
    [
        "order_invoice_number", "transaction_status", "transaction_id",
        "order_amount", "payment_method",
    ];

    public SePayService(IOptions<SePayOptions> options)
    {
        _options = options.Value;
    }

    public SePayFormData BuildFormData(CreatePaymentRequest request)
    {
        var fields = new Dictionary<string, string>
        {
            ["order_amount"] = request.OrderAmount.ToString(),
            ["merchant"] = _options.MerchantId,
            ["currency"] = "VND",
            ["operation"] = "PURCHASE",
            ["order_description"] = request.OrderDescription,
            ["order_invoice_number"] = request.OrderInvoiceNumber,
            ["success_url"] = _options.SuccessUrl,
            ["error_url"] = _options.ErrorUrl,
            ["cancel_url"] = _options.CancelUrl,
        };

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            fields["customer_id"] = request.CustomerId;

        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            fields["payment_method"] = request.PaymentMethod;

        return new SePayFormData
        {
            OrderAmount = fields["order_amount"],
            Merchant = fields["merchant"],
            Currency = fields["currency"],
            Operation = fields["operation"],
            OrderDescription = fields["order_description"],
            OrderInvoiceNumber = fields["order_invoice_number"],
            CustomerId = fields.TryGetValue("customer_id", out var cid) ? cid : null,
            PaymentMethod = fields.TryGetValue("payment_method", out var pm) ? pm : null,
            SuccessUrl = fields["success_url"],
            ErrorUrl = fields["error_url"],
            CancelUrl = fields["cancel_url"],
            Signature = SignFields(AllowedCheckoutFields, fields, includeAllInOrder: true),
            IsSandbox = _options.IsSandbox,
        };
    }

    public string CreateSignature(IDictionary<string, string> fields)
        => SignFields(AllowedCheckoutFields, fields, includeAllInOrder: true);

    public bool VerifyIpnSignature(IDictionary<string, string> fields, string providedSignature)
    {
        if (string.IsNullOrEmpty(providedSignature))
            return false;

        var expected = SignFields(AllowedIpnFields, fields, includeAllInOrder: false);
        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(providedSignature));
    }

    /// <summary>
    /// Builds the HMAC-SHA256 signature over the given fields.
    /// </summary>
    /// <param name="includeAllInOrder">
    /// When true (checkout, per PaymentAPI_Documentation.md), every allowed field is
    /// included in the fixed order with an empty value for missing keys. When false
    /// (IPN), only non-empty fields present in the payload are signed.
    /// </param>
    private string SignFields(string[] allowedFields, IDictionary<string, string> fields, bool includeAllInOrder)
    {
        var selected = includeAllInOrder
            ? allowedFields.Select(f => $"{f}={(fields.TryGetValue(f, out var v) ? v : string.Empty)}")
            : allowedFields
                .Where(f => fields.TryGetValue(f, out var v) && !string.IsNullOrEmpty(v))
                .Select(f => $"{f}={fields[f]}");

        var signedString = string.Join(",", selected);

        var keyBytes = Encoding.UTF8.GetBytes(_options.SecretKey);
        var dataBytes = Encoding.UTF8.GetBytes(signedString);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(dataBytes));
    }
}
