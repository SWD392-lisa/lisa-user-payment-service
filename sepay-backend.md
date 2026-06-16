# Tích hợp SePay — Hướng dẫn Backend (.NET)

> **Đối tượng:** Lập trình viên Backend — ASP.NET Core  
> **Vai trò:** Nhận yêu cầu từ frontend React, tạo signature, trả về form data, và xử lý IPN (webhook) từ SePay.

---

## Tổng quan luồng Backend

```
[React gửi POST /api/payment/create]
        ↓
[Backend tạo signature HMAC-SHA256]
        ↓
[Backend trả về SepayFormData (JSON) cho React]
        ↓
[SePay gọi IPN webhook → POST /api/payment/ipn]
        ↓
[Backend verify signature → cập nhật DB]
```

---

## Cấu hình — `appsettings.json`

```json
{
  "SePay": {
    "MerchantId": "MERCHANT_123",
    "SecretKey": "your-secret-key-here",
    "IsSandbox": true,
    "SuccessUrl": "https://yoursite.com/payment/success",
    "ErrorUrl": "https://yoursite.com/payment/error",
    "CancelUrl": "https://yoursite.com/payment/cancel"
  }
}
```

> ⚠️ Không commit `SecretKey` lên git. Dùng **User Secrets** khi dev local và **environment variables** khi deploy.

Đăng ký config trong `Program.cs`:

```csharp
builder.Services.Configure<SePayOptions>(
    builder.Configuration.GetSection("SePay"));
```

---

## Model & Options

```csharp
// SePayOptions.cs
public class SePayOptions
{
    public string MerchantId { get; set; } = string.Empty;
    public string SecretKey  { get; set; } = string.Empty;
    public bool   IsSandbox  { get; set; } = true;
    public string SuccessUrl { get; set; } = string.Empty;
    public string ErrorUrl   { get; set; } = string.Empty;
    public string CancelUrl  { get; set; } = string.Empty;
}

// Request từ React
public class CreatePaymentRequest
{
    public string OrderInvoiceNumber { get; set; } = string.Empty;
    public long   OrderAmount        { get; set; }
    public string OrderDescription   { get; set; } = string.Empty;
    public string? CustomerId        { get; set; }
}

// Response trả về React
public class SePayFormData
{
    public string OrderAmount        { get; set; } = string.Empty;
    public string Merchant           { get; set; } = string.Empty;
    public string Currency           { get; set; } = "VND";
    public string Operation          { get; set; } = "PURCHASE";
    public string OrderDescription   { get; set; } = string.Empty;
    public string OrderInvoiceNumber { get; set; } = string.Empty;
    public string SuccessUrl         { get; set; } = string.Empty;
    public string ErrorUrl           { get; set; } = string.Empty;
    public string CancelUrl          { get; set; } = string.Empty;
    public string Signature          { get; set; } = string.Empty;
    public bool   IsSandbox          { get; set; }
}
```

---

## Bước 1 — Service tạo Signature

```csharp
// SePayService.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

public class SePayService
{
    private readonly SePayOptions _options;

    public SePayService(IOptions<SePayOptions> options)
    {
        _options = options.Value;
    }

    // Thứ tự field phải giữ nguyên — không sắp xếp lại
    private static readonly string[] AllowedFields =
    [
        "order_amount", "merchant", "currency", "operation",
        "order_description", "order_invoice_number", "customer_id",
        "payment_method", "success_url", "error_url", "cancel_url",
    ];

    public string CreateSignature(Dictionary<string, string> fields)
    {
        var parts = AllowedFields
            .Where(f => fields.TryGetValue(f, out var v) && !string.IsNullOrEmpty(v))
            .Select(f => $"{f}={fields[f]}");

        var signedString = string.Join(",", parts);

        var keyBytes  = Encoding.UTF8.GetBytes(_options.SecretKey);
        var dataBytes = Encoding.UTF8.GetBytes(signedString);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hash);
    }

    public SePayFormData BuildFormData(CreatePaymentRequest req)
    {
        var fields = new Dictionary<string, string>
        {
            ["order_amount"]         = req.OrderAmount.ToString(),
            ["merchant"]             = _options.MerchantId,
            ["currency"]             = "VND",
            ["operation"]            = "PURCHASE",
            ["order_description"]    = req.OrderDescription,
            ["order_invoice_number"] = req.OrderInvoiceNumber,
            ["success_url"]          = _options.SuccessUrl,
            ["error_url"]            = _options.ErrorUrl,
            ["cancel_url"]           = _options.CancelUrl,
        };

        if (!string.IsNullOrEmpty(req.CustomerId))
            fields["customer_id"] = req.CustomerId;

        return new SePayFormData
        {
            OrderAmount        = fields["order_amount"],
            Merchant           = fields["merchant"],
            Currency           = fields["currency"],
            Operation          = fields["operation"],
            OrderDescription   = fields["order_description"],
            OrderInvoiceNumber = fields["order_invoice_number"],
            SuccessUrl         = fields["success_url"],
            ErrorUrl           = fields["error_url"],
            CancelUrl          = fields["cancel_url"],
            Signature          = CreateSignature(fields),
            IsSandbox          = _options.IsSandbox,
        };
    }
}
```

Đăng ký service trong `Program.cs`:

```csharp
builder.Services.AddScoped<SePayService>();
```

---

## Bước 2 — Controller: Tạo Form Data

```csharp
// PaymentController.cs
[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly SePayService _sePayService;

    public PaymentController(SePayService sePayService)
    {
        _sePayService = sePayService;
    }

    // POST /api/payment/create
    [HttpPost("create")]
    public IActionResult Create([FromBody] CreatePaymentRequest request)
    {
        if (string.IsNullOrEmpty(request.OrderInvoiceNumber))
            return BadRequest(new { error = "Thiếu order_invoice_number" });

        if (request.OrderAmount <= 0)
            return BadRequest(new { error = "order_amount phải lớn hơn 0" });

        var formData = _sePayService.BuildFormData(request);
        return Ok(formData);
    }
}
```

**Request từ React:**

```json
{
  "orderInvoiceNumber": "INV_20231201_001",
  "orderAmount": 100000,
  "orderDescription": "Thanh toán đơn hàng #12345",
  "customerId": "CUST_001"
}
```

**Response trả về:**

```json
{
  "orderAmount": "100000",
  "merchant": "MERCHANT_123",
  "currency": "VND",
  "operation": "PURCHASE",
  "orderDescription": "Thanh toán đơn hàng #12345",
  "orderInvoiceNumber": "INV_20231201_001",
  "successUrl": "https://yoursite.com/payment/success",
  "errorUrl": "https://yoursite.com/payment/error",
  "cancelUrl": "https://yoursite.com/payment/cancel",
  "signature": "a1b2c3d4e5f6...",
  "isSandbox": true
}
```

> ⚠️ **CORS:** Nhớ cho phép origin của React app trong `Program.cs`:
> ```csharp
> builder.Services.AddCors(opt => opt.AddPolicy("React",
>     p => p.WithOrigins("http://localhost:3000").AllowAnyMethod().AllowAnyHeader()));
> app.UseCors("React");
> ```

---

## Bước 3 — Xử lý IPN (Webhook từ SePay)

SePay POST về IPN URL sau khi thanh toán xong. **Đây là nơi duy nhất đáng tin cậy để cập nhật trạng thái đơn hàng.**

```csharp
// IPN Request model
public class SePayIpnRequest
{
    public string? OrderInvoiceNumber { get; set; }
    public string? TransactionStatus  { get; set; }
    public string? TransactionId      { get; set; }
    public string? Signature          { get; set; }
    // Thêm các field khác SePay gửi về nếu cần
}

// Trong PaymentController
// POST /api/payment/ipn
[HttpPost("ipn")]
public async Task<IActionResult> Ipn([FromBody] SePayIpnRequest ipn)
{
    // 1. Verify signature
    var fields = new Dictionary<string, string>
    {
        ["order_invoice_number"] = ipn.OrderInvoiceNumber ?? "",
        ["transaction_status"]   = ipn.TransactionStatus ?? "",
        // Map đầy đủ các field SePay gửi về
    };

    var expectedSig = _sePayService.CreateSignature(fields);
    if (ipn.Signature != expectedSig)
        return BadRequest(new { error = "Invalid signature" });

    // 2. Xử lý kết quả
    if (ipn.TransactionStatus == "SUCCESS")
    {
        // Cập nhật DB — ví dụ với EF Core:
        // await _db.Orders
        //     .Where(o => o.InvoiceNumber == ipn.OrderInvoiceNumber)
        //     .ExecuteUpdateAsync(s => s
        //         .SetProperty(o => o.Status, "paid")
        //         .SetProperty(o => o.TransactionId, ipn.TransactionId));
    }

    // 3. Trả 200 để SePay biết đã nhận
    return Ok(new { received = true });
}
```

> ⚠️ **Idempotency:** Kiểm tra `TransactionId` đã xử lý chưa trước khi cập nhật DB, tránh xử lý IPN trùng lặp.

---

## Ví dụ chuỗi ký mẫu

Chuỗi trước khi hash:

```
order_amount=100000,merchant=MERCHANT_123,currency=VND,operation=PURCHASE,order_description=Thanh toán đơn hàng #12345,order_invoice_number=INV_20231201_001,success_url=https://yoursite.com/payment/success,error_url=https://yoursite.com/payment/error,cancel_url=https://yoursite.com/payment/cancel
```

---

## Tham số đầy đủ

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---------|------|----------|-------|
| `merchant` | string | ✅ | ID merchant |
| `currency` | string | ✅ | Chỉ hỗ trợ `VND` |
| `order_amount` | string | ✅ | Số tiền > 0 |
| `operation` | string | ✅ | `PURCHASE` hoặc `VERIFY` |
| `order_description` | string | ✅ | Mô tả đơn hàng |
| `order_invoice_number` | string | ✅ | Mã hóa đơn — **phải duy nhất** |
| `payment_method` | string | ❌ | `CARD`, `BANK_TRANSFER`, `NAPAS_BANK_TRANSFER` |
| `customer_id` | string | ❌ | ID khách hàng |
| `success_url` | string | ❌ | URL redirect khi thành công |
| `error_url` | string | ❌ | URL redirect khi lỗi |
| `cancel_url` | string | ❌ | URL redirect khi hủy |

---

## Các lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| Signature không khớp | Sai thứ tự field hoặc encode | Log `signedString` trước khi hash để so sánh |
| CORS error từ React | Chưa cấu hình CORS | Thêm `WithOrigins` cho domain React |
| IPN không nhận được | URL không public | Dùng ngrok khi dev: `ngrok http 5000` |
| `order_invoice_number` trùng | Invoice đã tồn tại | Sinh unique: `$"INV_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N[..6]}"` |

---

## Checklist trước khi go-live

- [ ] `IsSandbox: false` và dùng production `SecretKey`
- [ ] `SecretKey` lưu trong environment variables / Azure Key Vault, không trong appsettings
- [ ] IPN URL đã đăng ký trên dashboard SePay
- [ ] Có cơ chế idempotency cho IPN (tránh xử lý 2 lần)
- [ ] CORS production chỉ cho phép domain thực, không dùng `AllowAnyOrigin`
- [ ] Log đầy đủ IPN request để debug khi có sự cố
