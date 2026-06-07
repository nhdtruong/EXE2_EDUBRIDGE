# API Thông Báo Chung Giáo Viên (Teacher Notification API)

Module này cung cấp các API cho phép giáo viên lấy danh sách các lớp học họ giảng dạy và gửi thông báo chung cho toàn bộ phụ huynh của lớp học đó.

---

## 1. Lấy danh sách lớp học của Giáo viên

### Endpoint
```txt
GET /api/v1/teacher/notifications/classes
```

### Purpose
Lấy danh sách các lớp học đang hoạt động do giáo viên hiện tại phụ trách giảng dạy nhằm phục vụ lựa chọn khi gửi thông báo chung.

### Roles
```txt
TEACHER
```

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Success",
  "data": [
    {
      "classId": 5,
      "className": "IELTS Foundation K06"
    },
    {
      "classId": 12,
      "className": "IELTS Intermediate K02"
    }
  ]
}
```

### Error Cases
- **401 Unauthorized:** Giáo viên chưa đăng nhập.
- **500 Internal Server Error:** Lỗi hệ thống khi kết nối cơ sở dữ liệu.

---

## 2. Gửi thông báo chung cho cả lớp

### Endpoint
```txt
POST /api/v1/teacher/notifications/broadcast
```

### Purpose
Gửi thông báo chung đến toàn bộ phụ huynh có con đang học trong lớp học được chọn. Hệ thống tự động tạo các thông báo riêng biệt trong database và gửi realtime qua SignalR Hub.

### Roles
```txt
TEACHER
```

### Request Body
```json
{
  "classId": 5,
  "title": "Thông báo họp phụ huynh định kỳ",
  "content": "Kính gửi quý phụ huynh, trung tâm sẽ tổ chức họp phụ huynh vào lúc 9h00 sáng Chủ Nhật ngày 14/06/2026..."
}
```

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Gửi thông báo chung thành công",
  "data": true
}
```

### Validation Rules
- `classId`: Bắt buộc.
- `title`: Bắt buộc, không quá 200 ký tự.
- `content`: Bắt buộc.

### Error Cases
- **400 Bad Request:** Dữ liệu đầu vào thiếu hoặc không hợp lệ, hoặc giáo viên không giảng dạy lớp này, hoặc lớp học không tồn tại.
- **401 Unauthorized:** Giáo viên chưa đăng nhập.
- **500 Internal Server Error:** Lỗi hệ thống trong quá trình lưu thông báo hoặc gửi qua SignalR.
