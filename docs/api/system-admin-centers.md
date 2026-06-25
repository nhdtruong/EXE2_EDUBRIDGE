# System Admin Centers API

Tài liệu cho API quản lý Trung tâm dành cho Quản trị viên hệ thống (System Admin).

## Tạo mới Trung tâm

Cho phép System Admin tạo mới một trung tâm. Trung tâm mới được tạo không yêu cầu có `OwnerUserId` (Cho phép `NULL`).

- **Endpoint**: `POST /api/v1/system-admin/centers`
- **Roles**: `SYSTEM_ADMIN`
- **Security**: Bearer Token / Cookie Auth với policy `SystemAdminOnly`

### Request Format

```json
{
  "centerName": "Trung tâm tiếng Anh EduBridge",
  "email": "contact@edubridge.vn",
  "phoneNumber": "0123456789",
  "address": "123 Đường ABC, Quận XYZ",
  "projectId": null
}
```

### Response Format (Success - 200 OK)

```json
{
  "success": true,
  "message": "Tạo trung tâm thành công",
  "data": {
    "centerId": 10,
    "ownerUserId": null,
    "centerName": "Trung tâm tiếng Anh EduBridge",
    "email": "contact@edubridge.vn",
    "phoneNumber": "0123456789",
    "address": "123 Đường ABC, Quận XYZ",
    "status": "Active",
    "createdAt": "2026-06-18T12:00:00",
    "projectId": null,
    "ownerFullName": null,
    "projectName": null
  },
  "errors": null
}
```

### Validation Rules

- `CenterName`: Bắt buộc, tối đa 150 ký tự.
- `Email`: Tùy chọn, đúng định dạng email, tối đa 150 ký tự.
- `PhoneNumber`: Tùy chọn, tối đa 20 ký tự.
- `Address`: Tùy chọn, tối đa 255 ký tự.

### Error Cases

- **401 Unauthorized**: Lỗi khi chưa đăng nhập hoặc không lấy được `UserId`.
- **403 Forbidden**: Lỗi khi User không có Role `SYSTEM_ADMIN`.
- **400 Bad Request**: Lỗi validation dữ liệu gửi lên.
