# Payment API Documentation - SePay Integration

## Tổng quan luồng thanh toán (Payment Flow)

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Frontend      │────▶│  Backend API     │────▶│   SePay Gateway │
│   (React/Vue)   │◀────│  (ASP.NET Core)  │◀────│   (Payment)     │
└─────────────────┘     └──────────────────┘     └─────────────────┘
        │                       │                          │
        │                       │                          │
        ▼                       ▼                          ▼
   1. Gọi API tạo      2. Tạo checkout form        3. Người dùng thanh toán
      thanh toán           với SePay                   qua SePay
        │                       │                          │
        │                       │                          │
   4. Redirect đến      5. SePay redirect về       6. Callback xử lý
      SePay (form)      Frontend (success/fail)      kết quả thanh toán
```

---

## Cấu hình hệ thống (Configuration)

### 1. Appsettings.json

```json
{
  "SePaySettings": {
    "UrlSePay": "https://secure.sepay.vn/checkout",
    "MerchantId": "YOUR_MERCHANT_ID",
    "SecretKey": "YOUR_SECRET_KEY"
  },
  "BaseCallbackUrl": "https://yourdomain.com"
}
```

### 2. Dependency Injection (Program.cs)

```csharp
// Thêm vào Program.cs
builder.Services.Configure<SePaySettings>(
    builder.Configuration.GetSection("SePaySettings"));

builder.Services.AddScoped<ISePayService, SePayService>();
builder.Services.AddScoped<ITuitionService, TuitionService>();
```

### 3. Models Configuration

```csharp
// Models/Configurations/SePaySettings.cs
namespace FapWeb.Models.Configurations
{
    public class SePaySettings
    {
        public string UrlSePay { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }
}
```

---

## API Endpoints cho Frontend

### 1. Khởi tạo thanh toán online (Tạo form SePay)

**Endpoint:** `POST /tuition/payonline`

**Request:**
```typescript
// FormData (multipart/form-data)
{
  tuitionFeeId: "3fa85f64-5717-4562-b3fc-2c963f66afa6"  // Guid - ID khoản học phí
}
```

**Response - Thành công:**
```typescript
// Status: 200 OK
// Content-Type: text/html

// Trả về HTML form để redirect đến SePay
`
<!DOCTYPE html>
<html>
<head>
    <title>Redirecting to SePay...</title>
</head>
<body>
    <form id="sepayForm" action="https://secure.sepay.vn/checkout" method="POST">
        <input type="hidden" name="order_amount" value="5000000" />
        <input type="hidden" name="merchant" value="YOUR_MERCHANT_ID" />
        <input type="hidden" name="currency" value="VND" />
        <input type="hidden" name="operation" value="PURCHASE" />
        <input type="hidden" name="order_description" value="Thanh toan hoc phi thang 6/2026" />
        <input type="hidden" name="order_invoice_number" value="INV-20260614-001" />
        <input type="hidden" name="customer_id" value="student123" />
        <input type="hidden" name="payment_method" value="BANK_TRANSFER" />
        <input type="hidden" name="success_url" value="https://yourdomain.com/tuition/paymentcallback?invoice=INV-20260614-001&result=success" />
        <input type="hidden" name="error_url" value="https://yourdomain.com/tuition/paymentcallback?invoice=INV-20260614-001&result=failed" />
        <input type="hidden" name="cancel_url" value="https://yourdomain.com/tuition/paymentcallback?invoice=INV-20260614-001&result=cancel" />
        <input type="hidden" name="signature" value="BASE64_HMAC_SHA256_SIGNATURE" />
    </form>
    <script>document.getElementById('sepayForm').submit();</script>
</body>
</html>
`
```

**Response - Lỗi:**
```typescript
// Redirect đến /tuition/index với TempData["ErrorMessage"]
```

---

### 2. Callback sau khi thanh toán (SePay redirect về)

**Endpoint:** `GET /tuition/paymentcallback`

**Query Parameters:**
```typescript
{
  invoice: "INV-20260614-001",    // Mã hóa đơn
  result: "success" | "cancel" | "failed"  // Kết quả thanh toán
}
```

**Response - Thành công:**
```typescript
// Redirect đến /tuition/index
// TempData["SuccessMessage"] = "Thanh toán học phí qua SePay thành công."
```

**Response - Hủy:**
```typescript
// Redirect đến /tuition/index
// TempData["ErrorMessage"] = "Giao dịch đã bị hủy."
```

**Response - Thất bại:**
```typescript
// Redirect đến /tuition/index
// TempData["ErrorMessage"] = "Thanh toán không thành công hoặc giao dịch không hợp lệ."
```

---

### 3. Ghi nhận thanh toán thủ công (Cash/Transfer)

**Endpoint:** `POST /tuition/createpayment`

**Request:**
```typescript
// FormData (multipart/form-data)
{
  tuitionFeeId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",  // Guid
  studentId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",     // Guid
  studentName: "Nguyen Van A",                              // string
  remainingAmount: 5000000,                                 // decimal
  amount: 2000000,                                          // decimal (must be > 0)
  paymentDate: "2026-06-14",                                // date
  note: "Thanh toán phần còn lại tháng 6"                    // string (optional)
}
```

**Response - Thành công:**
```typescript
// Redirect đến /tuition/index
// TempData["SuccessMessage"] = "Payment recorded successfully."
```

**Response - Lỗi Validation:**
```typescript
// Status: 400 Bad Request
// Trả về lại View với ModelState errors
```

**Response - Lỗi Server:**
```typescript
// Redirect đến /tuition/index
// TempData["ErrorMessage"] = "Unable to record payment."
```

---

### 4. Lấy lịch sử thanh toán

**Endpoint:** `GET /tuition/history`

**Query Parameters (optional):**
```typescript
{
  tuitionFeeId: "3fa85f64-5717-4562-b3fc-2c963f66afa6"  // Guid (optional) - Lọc theo khoản học phí cụ thể
}
```

**Response:**
```typescript
// Status: 200 OK
// Content-Type: text/html (View)

// Trả về View với model:
{
  payments: [
    {
      transactionId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      amount: 2000000,
      paymentDate: "2026-06-14",
      paymentMethod: "CASH" | "BANK_TRANSFER" | "ONLINE_SEPAY",
      note: "Ghi chú thanh toán",
      createdByName: "Admin User",
      tuitionMonth: "6/2026",
      studentName: "Nguyen Van A"
    }
  ]
}
```

---

### 5. Lấy danh sách học phí

**Endpoint:** `GET /tuition`

**Response:**
```typescript
// Status: 200 OK
// Content-Type: text/html (View)

// Trả về View với model:
{
  tuitionStatuses: [
    {
      tuitionFeeId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      studentId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      studentName: "Nguyen Van A",
      className: "10A1",
      monthYear: "6/2026",
      amount: 5000000,
      paidAmount: 2000000,
      remainingAmount: 3000000,
      status: "PENDING" | "PARTIAL" | "PAID" | "OVERPAID",
      dueDate: "2026-06-30",
      canPayOnline: true
    }
  ]
}
```

---

## DTOs Reference

### SePayCheckoutOrderDto
```csharp
public class SePayCheckoutOrderDto
{
    public decimal Amount { get; set; }                    // Số tiền thanh toán
    public string Description { get; set; } = string.Empty; // Mô tả đơn hàng
    public string InvoiceNumber { get; set; } = string.Empty; // Mã hóa đơn
    public string? CustomerId { get; set; }                   // ID khách hàng
    public string? PaymentMethod { get; set; }                  // Phương thức thanh toán
    public string? SuccessUrl { get; set; }                     // URL khi thành công
    public string? ErrorUrl { get; set; }                     // URL khi lỗi
    public string? CancelUrl { get; set; }                      // URL khi hủy
}
```

### SePayCheckoutFormDto
```csharp
public class SePayCheckoutFormDto
{
    public string ActionUrl { get; set; } = string.Empty;                              // URL SePay checkout
    public List<KeyValuePair<string, string>> Fields { get; set; } = new();           // Các field ẩn
}
```

### PaymentCreateDto
```csharp
public class PaymentCreateDto
{
    public Guid TuitionFeeId { get; set; }           // ID khoản học phí
    public Guid StudentId { get; set; }              // ID học sinh
    public string StudentName { get; set; } = string.Empty; // Tên học sinh
    public decimal RemainingAmount { get; set; }     // Số tiền còn lại
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }              // Số tiền thanh toán
    
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today; // Ngày thanh toán
    
    public string? Note { get; set; }                 // Ghi chú
}
```

### PaymentHistoryDto
```csharp
public class PaymentHistoryDto
{
    public Guid TransactionId { get; set; }          // ID giao dịch
    public decimal Amount { get; set; }                // Số tiền
    public DateTime PaymentDate { get; set; }          // Ngày thanh toán
    public string PaymentMethod { get; set; } = string.Empty; // Phương thức
    public string? Note { get; set; }                   // Ghi chú
    public string? CreatedByName { get; set; }        // Người tạo
    public string? TuitionMonth { get; set; }           // Tháng học phí
    public string? StudentName { get; set; }          // Tên học sinh
}
```

---

## Cách tính Signature cho SePay

```csharp
private static string SignFields(IEnumerable<KeyValuePair<string, string>> fields, string secretKey)
{
    // Bước 1: Join các field theo format: key1=value1,key2=value2,...
    var signedString = string.Join(",", 
        fields.Select(f => $"{f.Key}={f.Value}"));

    // Bước 2: Tạo HMAC-SHA256 hash
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedString));
    
    // Bước 3: Convert sang Base64
    return Convert.ToBase64String(hash);
}
```

**Thứ tự các field phải đúng:**
1. `order_amount`
2. `merchant`
3. `currency`
4. `operation`
5. `order_description`
6. `order_invoice_number`
7. `customer_id`
8. `payment_method`
9. `success_url`
10. `error_url`
11. `cancel_url`
12. `signature` (thêm sau cùng)

---

## Ví dụ Flow thanh toán hoàn chỉnh

### Bước 1: Frontend gọi API tạo thanh toán

```javascript
// JavaScript/TypeScript
async function createOnlinePayment(tuitionFeeId) {
    const formData = new FormData();
    formData.append('tuitionFeeId', tuitionFeeId);
    
    const response = await fetch('/tuition/payonline', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: formData
    });
    
    // Nhận HTML form và tự động submit
    const html = await response.text();
    document.body.insertAdjacentHTML('beforeend', html);
    document.getElementById('sepayForm').submit();
}
```

### Bước 2: Backend tạo checkout form

```csharp
public async Task<SePayCheckoutFormDto> CreateOnlinePaymentAsync(
    Guid tuitionFeeId, Guid userId, string roleName, string baseCallbackUrl)
{
    // 1. Kiểm tra quyền và lấy thông tin học phí
    var tuitionFee = await _tuitionFeeRepository.GetByIdAsync(tuitionFeeId);
    
    // 2. Tạo transaction record (trạng thái PENDING)
    var transaction = new Transaction
    {
        TransactionId = Guid.NewGuid(),
        TuitionFeeId = tuitionFeeId,
        Amount = tuitionFee.RemainingAmount,
        PaymentMethod = "ONLINE_SEPAY",
        Status = "PENDING",
        CreatedAt = DateTime.UtcNow
    };
    await _transactionRepository.AddAsync(transaction);
    
    // 3. Tạo SePay order
    var order = new SePayCheckoutOrderDto
    {
        Amount = tuitionFee.RemainingAmount,
        Description = $"Thanh toan hoc phi thang {tuitionFee.Month}/{tuitionFee.Year}",
        InvoiceNumber = transaction.TransactionId.ToString("N").Substring(0, 20),
        CustomerId = tuitionFee.StudentId.ToString(),
        PaymentMethod = "BANK_TRANSFER",
        SuccessUrl = $"{baseCallbackUrl}/tuition/paymentcallback?invoice={transaction.TransactionId}&result=success",
        ErrorUrl = $"{baseCallbackUrl}/tuition/paymentcallback?invoice={transaction.TransactionId}&result=failed",
        CancelUrl = $"{baseCallbackUrl}/tuition/paymentcallback?invoice={transaction.TransactionId}&result=cancel"
    };
    
    // 4. Build checkout form
    return _sePayService.BuildCheckoutForm(order);
}
```

### Bước 3: Callback xử lý kết quả

```csharp
[HttpGet]
public async Task<IActionResult> PaymentCallback(string invoice, string result)
{
    // Map result sang status
    var statusName = result?.ToLowerInvariant() switch
    {
        "success" => "SUCCESS",
        "cancel" => "CANCELLED",
        _ => "FAILED"
    };
    
    // Cập nhật transaction và tuition fee
    var isSuccess = await _tuitionService.FinalizeOnlinePaymentAsync(invoice, statusName);
    
    // Redirect với thông báo
    if (isSuccess)
    {
        TempData["SuccessMessage"] = "Thanh toán học phí qua SePay thành công.";
    }
    else
    {
        TempData["ErrorMessage"] = statusName == "CANCELLED"
            ? "Giao dịch đã bị hủy."
            : "Thanh toán không thành công hoặc giao dịch không hợp lệ.";
    }
    
    return RedirectToAction(nameof(Index));
}
```

---

## Bảng trạng thái giao dịch

| Status | Mô tả |
|--------|-------|
| `PENDING` | Giao dịch đang chờ xử lý |
| `SUCCESS` | Thanh toán thành công |
| `FAILED` | Thanh toán thất bại |
| `CANCELLED` | Người dùng hủy giao dịch |

---

## Các phương thức thanh toán

| PaymentMethod | Mô tả |
|---------------|-------|
| `CASH` | Thanh toán tiền mặt |
| `BANK_TRANSFER` | Chuyển khoản ngân hàng |
| `ONLINE_SEPAY` | Thanh toán online qua SePay |

---

## Xử lý lỗi (Error Handling)

### Mã lỗi phổ biến:

| Mã lỗi | Mô tả | Cách xử lý |
|--------|-------|------------|
| `INVALID_SIGNATURE` | Chữ ký không hợp lệ | Kiểm tra SecretKey và thứ tự fields |
| `INVALID_AMOUNT` | Số tiền không hợp lệ | Kiểm tra amount > 0 |
| `DUPLICATE_INVOICE` | Mã hóa đơn trùng | Tạo mã hóa đơn mới |
| `TRANSACTION_NOT_FOUND` | Không tìm thấy giao dịch | Kiểm tra invoice ID |
| `PAYMENT_ALREADY_COMPLETED` | Đã thanh toán trước đó | Kiểm tra trạng thái PENDING |

---

## Sequence Diagram

```
Frontend                Backend                 Database              SePay
   │                       │                       │                    │
   │  1. POST /payonline   │                       │                    │
   │──────────────────────▶│                       │                    │
   │                       │  2. Validate request  │                    │
   │                       │                       │                    │
   │                       │  3. Create Transaction│                    │
   │                       │──────────────────────▶│                    │
   │                       │◀──────────────────────│                    │
   │                       │                       │                    │
   │                       │  4. Build SePay form   │                    │
   │                       │  5. Sign with HMAC    │                    │
   │                       │                       │                    │
   │◀──────────────────────│  6. Return HTML form  │                    │
   │                       │                       │                    │
   │  7. Auto-submit form  │                       │                    │
   │──────────────────────────────────────────────────────────────────▶│
   │                       │                       │                    │
   │                       │                       │    8. User pays    │
   │                       │                       │                    │
   │◀──────────────────────────────────────────────────────────────────│
   │  9. Redirect to callback                         10. SePay processes│
   │                       │                       │                    │
   │                       │  11. GET /paymentcallback                   │
   │                       │──────────────────────▶│                    │
   │                       │                       │                    │
   │                       │  12. Update Transaction│                    │
   │                       │──────────────────────▶│                    │
   │                       │◀──────────────────────│                    │
   │                       │                       │                    │
   │                       │  13. Update TuitionFee │                    │
   │                       │──────────────────────▶│                    │
   │                       │◀──────────────────────│                    │
   │                       │                       │                    │
   │◀──────────────────────│  14. Redirect to index │                    │
   │                       │                       │                    │
```

---

## Lưu ý quan trọng

1. **Thứ tự fields trong SePay**: Phải đúng thứ tự khi tạo signature
2. **SecretKey**: Phải giữ bảo mật, không commit vào git
3. **Callback URL**: Phải public để SePay redirect về được
4. **Idempotent**: Cần xử lý tránh duplicate transaction
5. **Validation**: Luôn validate signature từ SePay (nếu có callback từ server)
