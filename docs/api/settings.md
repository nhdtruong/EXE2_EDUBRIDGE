# Center Settings API

Tài liệu API quản lý cấu hình trung tâm (Settings). Bao gồm các thiết lập chung và tài chính.

## Endpoints

### 1. Lấy thông tin cấu hình trung tâm
* **Endpoint:** `GET /api/v1/owner/settings`
* **Purpose:** Lấy cấu hình hiện tại của trung tâm.
* **Roles:** `OWNER`

**Request:** None (Dùng Token JWT hoặc Cookie)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "general": {
      "centerName": "EduBridge HN",
      "address": "123 Cau Giay",
      "phoneNumber": "0123456789",
      "email": "contact@edubridge.vn",
      "timeZone": "Asia/Ho_Chi_Minh",
      "workingDays": "2,3,4,5,6,7",
      "logoUrl": null
    },
    "finance": {
      "defaultInvoiceDueDays": 7,
      "paymentMethods": "CASH,TRANSFER",
      "receiptNumberingFormat": "BL-{YYYYMMDD}-{ID}",
      "currency": "VND",
      "showTaxOnInvoice": false
    }
  }
}
```

**Error Cases:**
* `401 Unauthorized`: Không xác định được CenterId hoặc User.
* `404 Not Found`: Không tìm thấy center tương ứng.

---

### 2. Cập nhật cấu hình trung tâm
* **Endpoint:** `PUT /api/v1/owner/settings`
* **Purpose:** Lưu các thay đổi cấu hình trung tâm.
* **Roles:** `OWNER`

**Request Body:**
```json
{
  "general": {
    "centerName": "EduBridge HN - Updated",
    "address": "123 Cau Giay",
    "phoneNumber": "0123456789",
    "email": "contact@edubridge.vn",
    "timeZone": "Asia/Ho_Chi_Minh",
    "workingDays": "2,3,4,5,6,7",
    "logoUrl": null
  },
  "finance": {
    "defaultInvoiceDueDays": 10,
    "paymentMethods": "CASH,TRANSFER,CREDIT_CARD",
    "receiptNumberingFormat": "BL-{YYYYMMDD}-{ID}",
    "currency": "VND",
    "showTaxOnInvoice": true
  }
}
```

**Validation Rules:**
* `general.centerName`: Bắt buộc nhập.
* `finance.defaultInvoiceDueDays`: Bắt buộc từ 0 đến 365.

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Cập nhật cấu hình thành công."
}
```

**Error Cases:**
* `400 BadRequest`: Dữ liệu không hợp lệ (Validation failed).
* `401 Unauthorized`: Không có quyền.
