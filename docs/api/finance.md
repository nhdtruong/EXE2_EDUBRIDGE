# Finance API Documentation

## Module Overview
Quản lý Tài chính: Hóa đơn (Invoices), Thanh toán (Payments), Biên lai (Receipts), Thống kê (Finance Summary).
Mọi thao tác đều liên kết chặt chẽ và lưu lịch sử giao dịch rõ ràng. Sử dụng Transaction để đảm bảo tính nhất quán dữ liệu khi thanh toán/hủy thanh toán.

---

## 1. Hóa đơn (Invoices)

### 1.1. Lấy danh sách hóa đơn
**Endpoint:** `GET /api/v1/owner/invoices`
**Roles:** `OWNER`
**Purpose:** Lấy danh sách hóa đơn của trung tâm. Hỗ trợ phân trang, tìm kiếm theo tên học sinh/mã hóa đơn và lọc theo trạng thái.

**Request Query Parameters:**
- `centerId` (int, required): ID của trung tâm
- `InvoiceCode` (string, optional): Tìm kiếm theo Mã hóa đơn
- `StudentName` (string, optional): Tìm kiếm theo Tên học sinh
- `ClassId` (int, optional): Lọc theo ID lớp học
- `Status` (string, optional): Trạng thái (`Unpaid`, `Partial`, `Paid`, `Cancelled`)
- `DateFrom` (string, optional): Lọc từ ngày (định dạng YYYY-MM-DD)
- `DateTo` (string, optional): Lọc đến ngày (định dạng YYYY-MM-DD)
- `PageNumber` (int, optional): Trang hiện tại, mặc định 1
- `PageSize` (int, optional): Số item trên 1 trang. Giá trị hợp lệ: `10`, `20`, `50`, `100`, `200`, `500`. Mặc định API là `10`; nếu truyền sai service sẽ dùng `20`.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "invoiceId": 1,
      "invoiceCode": "HD-202606-0001",
      "studentId": 5,
      "studentName": "Nguyễn Văn A",
      "studentCode": "HS005",
      "classId": 2,
      "className": "IELTS Foundation K01",
      "courseId": 1,
      "courseName": "IELTS Foundation",
      "amount": 5000000,
      "discountAmount": 0,
      "finalAmount": 5000000,
      "dueDate": "2026-06-15",
      "status": "UNPAID",
      "paidAmount": 0,
      "remainingAmount": 5000000,
      "createdAt": "2026-06-08T10:00:00Z",
      "createdByUserName": "owner1"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### 1.2. Lấy thông tin chi tiết hóa đơn
**Endpoint:** `GET /api/v1/owner/invoices/{invoiceId}`
**Roles:** `OWNER`
**Purpose:** Lấy thông tin chi tiết hóa đơn và danh sách các lần thanh toán liên quan.

**Response:**
```json
{
  "success": true,
  "data": {
    "invoice": { /* Invoice Response Object */ },
    "payments": [
      {
        "paymentId": 1,
        "amount": 2000000,
        "paymentMethod": "CASH",
        "transactionReference": null,
        "status": "SUCCESS",
        "createdAt": "2026-06-08T10:30:00Z",
        "receivedByUserName": "owner1"
      }
    ]
  }
}
```

### 1.3. Lấy công nợ học sinh
**Endpoint:** `GET /api/v1/owner/invoices/student-debts`
**Roles:** `OWNER`
**Purpose:** Lấy danh sách các khoản nợ của học sinh trong trung tâm, có thể lọc theo lớp học cụ thể.

**Request Query Parameters:**
- `centerId` (int, required): ID của trung tâm
- `ClassId` (int, optional): Lọc theo lớp học
- `PageNumber` (int, optional)
- `PageSize` (int, optional)

---

## 2. Thanh toán (Payments)

### 2.1. Ghi nhận thanh toán
**Endpoint:** `POST /api/v1/owner/payments`
**Roles:** `OWNER`
**Purpose:** Ghi nhận học sinh đã đóng tiền (một phần hoặc toàn bộ).

**Request Body:**
```json
{
  "invoiceId": 1,
  "amount": 2500000,
  "paymentMethod": "CASH",
  "transactionReference": ""
}
```
**Validation Rules:**
- `amount` > 0
- `paymentMethod` thuộc `["CASH", "BANK_TRANSFER"]`
- Nếu `amount` > `RemainingAmount` của hóa đơn -> Lỗi "Số tiền thanh toán vượt quá số dư nợ của hóa đơn."

**Response:**
```json
{
  "success": true,
  "message": "Thanh toán thành công. Trạng thái hóa đơn: PARTIALLY_PAID",
  "data": 1 // PaymentId
}
```

### 2.2. Hủy thanh toán
**Endpoint:** `POST /api/v1/owner/payments/{paymentId}/cancel`
**Roles:** `OWNER`
**Purpose:** Hủy bỏ một khoản thanh toán do nhập sai hoặc giao dịch lỗi. Sẽ tự động tính toán lại công nợ và cập nhật trạng thái hóa đơn.

**Request Body:**
```json
{
  "reason": "Nhập sai số tiền"
}
```

**Validation Rules:**
- Chỉ thanh toán có trạng thái `SUCCESS` mới được hủy.
- Không thể hủy nếu thanh toán này đã được xuất Biên lai (`Receipt`). (Phải hủy Biên lai trước).

**Error Cases:**
- Lỗi `Payment already has a receipt`: Bắt buộc hủy Receipt trước khi hủy Payment.

**Response:**
```json
{
  "success": true,
  "message": "Hủy giao dịch thanh toán thành công."
}
```

---

## 3. Biên lai (Receipts)

### 3.1. Xuất biên lai
**Endpoint:** `POST /api/v1/owner/receipts`
**Roles:** `OWNER`
**Purpose:** Phát hành biên lai cho một khoản thanh toán hợp lệ.

**Request Body:**
```json
{
  "paymentId": 1,
  "receiptMethod": "PRINT",
  "description": "Biên lai thu học phí đợt 1"
}
```

**Validation Rules:**
- `paymentId` phải tồn tại và có trạng thái `SUCCESS`.
- `receiptMethod` thuộc `["PRINT", "EMAIL"]`.

**Response:**
```json
{
  "success": true,
  "message": "Phát hành biên lai thành công.",
  "data": {
    "receiptId": 1,
    "receiptCode": "BL-202606-0001",
    "issuedAt": "2026-06-08T11:00:00Z"
  }
}
```

### 3.2. Hủy biên lai (Void Receipt)
**Endpoint:** `POST /api/v1/owner/receipts/{receiptId}/void`
**Roles:** `OWNER`
**Purpose:** Hủy biên lai do in sai thông tin.

**Request Body:**
```json
{
  "reason": "In sai thông tin người nộp"
}
```

**Validation Rules:**
- Biên lai đang ở trạng thái `ISSUED` mới được void.

**Response:**
```json
{
  "success": true,
  "message": "Hủy biên lai thành công."
}
```

---

## 4. Báo cáo tài chính (Finance Summary)

### 4.1. Thống kê Dashboard
**Endpoint:** `GET /api/v1/owner/finance/summary/dashboard`
**Roles:** `OWNER`
**Purpose:** Lấy các chỉ số doanh thu và công nợ trên màn hình chính của Quản lý Tài chính.

**Request Query Parameters:**
- `centerId` (int, required)
- `month` (int, required)
- `year` (int, required)

**Response:**
```json
{
  "success": true,
  "data": {
    "totalRevenue": 412000000,
    "totalDebt": 73000000,
    "totalInvoicesCreated": 100,
    "totalInvoicesPaid": 85
  }
}
```

### 4.2. Công nợ theo lớp
**Endpoint:** `GET /api/v1/owner/finance/summary/class-debts`
**Roles:** `OWNER`
**Purpose:** Thống kê khoản thu và công nợ được nhóm theo từng lớp học.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "classId": 2,
      "className": "IELTS Foundation K01",
      "totalExpected": 50000000,
      "totalCollected": 45000000,
      "totalDebt": 5000000,
      "unpaidStudentsCount": 1
    }
  ]
}
```
