# Luồng Thanh Toán SePay — ProjectLucy

## Tổng quan

```
Frontend           Backend (.NET)              SePay Gateway
   │                    │                           │
   │  1. POST /create   │                           │
   │───────────────────▶│  2. Lưu transaction       │
   │                    │     (status=pending)       │
   │  3. Trả form data  │                           │
   │◀───────────────────│                           │
   │                    │                           │
   │  4. Submit form ──────────────────────────────▶│
   │                    │                           │  5. User thanh toán
   │◀────────────── 6. Redirect (success/error/cancel)
   │                    │                           │
   │  7. POST /confirm  │                           │
   │───────────────────▶│  8. Cập nhật transaction  │
   │                    │     + cộng wallet          │
   │                    │                           │
   │                    │◀── 9. POST /ipn (webhook) │
   │                    │  10. Verify sig + update   │
   │                    │      + cộng wallet         │
```

> **Confirm vs IPN:** Cả hai đều cập nhật transaction + cộng wallet, nhưng IPN là kênh chính (server-to-server, đáng tin). Confirm là fallback cho sandbox/dev khi backend không public.

---

## Cấu hình

`appsettings.json`:

```json
{
  "SePay": {
    "UrlSePay": "https://pay-sandbox.sepay.vn/v1/checkout/init",
    "MerchantId": "SP-TEST-XXXXXX",
    "SecretKey": "spsk_test_xxxxx",
    "IsSandbox": true,
    "SuccessUrl": "https://your-frontend.com/payment/success",
    "ErrorUrl": "https://your-frontend.com/payment/error",
    "CancelUrl": "https://your-frontend.com/payment/cancel"
  }
}
```

| Field | Mô tả |
|---|---|
| `UrlSePay` | URL checkout gateway (sandbox hoặc production) |
| `MerchantId` | Mã merchant từ dashboard SePay |
| `SecretKey` | Secret key để tạo HMAC-SHA256 signature |
| `IsSandbox` | `true` = sandbox, `false` = production |
| `SuccessUrl` / `ErrorUrl` / `CancelUrl` | URL frontend nhận redirect sau thanh toán |

---

## API Endpoints

### 1. Tạo thanh toán — `POST /api/payment/create`

**Auth:** Bearer token (required)

**Request body:**

```json
{
  "orderInvoiceNumber": "INV_20260629_abc123",
  "orderAmount": 50000,
  "orderDescription": "Nap tien vi Lucy",
  "customerId": "user-uuid-optional",
  "paymentMethod": "CARD"
}
```

| Field | Type | Required | Note |
|---|---|---|---|
| `orderInvoiceNumber` | string | Yes | Mã hóa đơn duy nhất (max 100 ký tự) |
| `orderAmount` | long | Yes | Số tiền (VND), phải > 0 |
| `orderDescription` | string | Yes | Mô tả đơn hàng (max 255) |
| `customerId` | string | No | ID khách hàng |
| `paymentMethod` | string | No | `CARD`, `BANK_TRANSFER`, `NAPAS_BANK_TRANSFER` |

**Response (200):**

```json
{
  "status": 200,
  "message": "Payment form data created",
  "data": {
    "orderAmount": "50000",
    "merchant": "SP-TEST-XXXXXX",
    "currency": "VND",
    "operation": "PURCHASE",
    "orderDescription": "Nap tien vi Lucy",
    "orderInvoiceNumber": "INV_20260629_abc123",
    "customerId": "user-uuid-optional",
    "paymentMethod": "CARD",
    "successUrl": "https://your-frontend.com/payment/success?orderInvoiceNumber=INV_20260629_abc123&orderAmount=50000&transactionId=42",
    "errorUrl": "https://your-frontend.com/payment/error?orderInvoiceNumber=...&orderAmount=...&transactionId=...",
    "cancelUrl": "https://your-frontend.com/payment/cancel?orderInvoiceNumber=...&orderAmount=...&transactionId=...",
    "signature": "base64-hmac-sha256-string",
    "isSandbox": true
  }
}
```

**Backend xử lý:**
1. Resolve `transaction_type` code = `ONLINE_SEPAY`
2. Tạo record `Transaction` với `status = "pending"`
3. Gọi `SePayService.BuildFormData()` → ký HMAC-SHA256 → trả `SePayFormData`

---

### 2. Frontend submit form đến SePay

Frontend nhận `SePayFormData`, tạo hidden form POST đến `UrlSePay`:

```html
<form id="sepayForm" action="https://pay-sandbox.sepay.vn/v1/checkout/init" method="POST">
  <input type="hidden" name="order_amount" value="50000" />
  <input type="hidden" name="merchant" value="SP-TEST-XXXXXX" />
  <input type="hidden" name="currency" value="VND" />
  <input type="hidden" name="operation" value="PURCHASE" />
  <input type="hidden" name="order_description" value="Nap tien vi Lucy" />
  <input type="hidden" name="order_invoice_number" value="INV_20260629_abc123" />
  <input type="hidden" name="success_url" value="https://..." />
  <input type="hidden" name="error_url" value="https://..." />
  <input type="hidden" name="cancel_url" value="https://..." />
  <input type="hidden" name="signature" value="base64-hmac-sha256" />
</form>
<script>document.getElementById('sepayForm').submit();</script>
```

Hoặc dùng React redirect:

```tsx
function redirectToSePay(formData: SePayFormData) {
  const url = formData.isSandbox
    ? "https://pay-sandbox.sepay.vn/v1/checkout/init"
    : "https://pay.sepay.vn/v1/checkout/init";

  const form = document.createElement("form");
  form.method = "POST";
  form.action = url;

  const fields = {
    order_amount: formData.orderAmount,
    merchant: formData.merchant,
    currency: formData.currency,
    operation: formData.operation,
    order_description: formData.orderDescription,
    order_invoice_number: formData.orderInvoiceNumber,
    customer_id: formData.customerId,
    payment_method: formData.paymentMethod,
    success_url: formData.successUrl,
    error_url: formData.errorUrl,
    cancel_url: formData.cancelUrl,
    signature: formData.signature,
  };

  for (const [key, value] of Object.entries(fields)) {
    if (!value) continue;
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = key;
    input.value = value;
    form.appendChild(input);
  }

  document.body.appendChild(form);
  form.submit();
}
```

---

### 3. Confirm thanh toán (frontend callback) — `POST /api/payment/confirm`

**Auth:** Bearer token (required)

Sau khi SePay redirect user về `successUrl`, frontend đọc query params rồi gọi:

```json
{
  "orderInvoiceNumber": "INV_20260629_abc123",
  "transactionId": "42",
  "amount": "50000",
  "status": "success"
}
```

| Field | Note |
|---|---|
| `status` | `"success"` / `"failed"` / `"cancelled"` |

**Backend xử lý:**
1. Tìm transaction theo `orderInvoiceNumber`
2. Kiểm tra transaction thuộc về user đang đăng nhập
3. Idempotency: nếu transaction đã ở trạng thái cuối → trả về ngay
4. Map status: `success → completed`, `failed → failed`, `cancelled → cancelled`
5. Nếu `completed`: cộng tiền vào wallet + ghi ledger entry

---

### 4. IPN Webhook (server-to-server) — `POST /api/payment/ipn`

**Auth:** Không cần (AllowAnonymous), verify bằng signature

SePay gọi endpoint này sau khi xử lý thanh toán:

```json
{
  "order_invoice_number": "INV_20260629_abc123",
  "transaction_status": "success",
  "transaction_id": "sepay-txn-id",
  "order_amount": "50000",
  "payment_method": "CARD",
  "signature": "base64-hmac-sha256"
}
```

> **Lưu ý:** Body dùng `snake_case` (khác với các API khác dùng `camelCase`).

**Backend xử lý:**
1. **Verify signature** — build lại HMAC-SHA256 từ các field, so sánh constant-time
2. Tìm transaction theo `order_invoice_number`
3. Idempotency: đã ở trạng thái cuối → trả 200 ngay
4. Map status SePay → internal
5. Nếu `completed`: cộng wallet + ghi ledger
6. **Luôn trả 200** để SePay không retry

---

## Cách tính Signature

### Checkout (tạo form)

Ghép **tất cả** các field theo thứ tự cố định (field thiếu thì value rỗng):

```
order_amount=50000,merchant=SP-TEST-XXXXXX,currency=VND,operation=PURCHASE,order_description=Nap tien vi Lucy,order_invoice_number=INV_xxx,customer_id=,payment_method=CARD,success_url=https://...,error_url=https://...,cancel_url=https://...
```

→ HMAC-SHA256 với `SecretKey` → Base64

### IPN (verify webhook)

Chỉ ghép các field **có giá trị** theo thứ tự:

```
order_invoice_number=INV_xxx,transaction_status=success,transaction_id=sepay-txn-id,order_amount=50000,payment_method=CARD
```

→ HMAC-SHA256 với `SecretKey` → Base64 → so sánh constant-time với `signature` trong request

**Thứ tự field bắt buộc:**

| Checkout | IPN |
|---|---|
| order_amount | order_invoice_number |
| merchant | transaction_status |
| currency | transaction_id |
| operation | order_amount |
| order_description | payment_method |
| order_invoice_number | |
| customer_id | |
| payment_method | |
| success_url | |
| error_url | |
| cancel_url | |

---

## Trạng thái Transaction

| SePay trả về | DB lưu | Wallet |
|---|---|---|
| `success` | `completed` | Cộng tiền |
| `failed` | `failed` | Không thay đổi |
| `cancelled` | `cancelled` | Không thay đổi |

---

## Tóm tắt luồng Frontend

```
1. User bấm "Nạp tiền"
2. Frontend gọi POST /api/payment/create (có Bearer token)
3. Nhận SePayFormData JSON
4. Tạo hidden form, submit đến SePay gateway
5. User thanh toán trên trang SePay
6. SePay redirect về successUrl/errorUrl/cancelUrl
7. Frontend đọc query params → gọi POST /api/payment/confirm
8. Hiển thị kết quả cho user
```

Song song, SePay cũng gọi IPN webhook (`POST /api/payment/ipn`) server-to-server để đảm bảo cập nhật dù frontend không confirm.
