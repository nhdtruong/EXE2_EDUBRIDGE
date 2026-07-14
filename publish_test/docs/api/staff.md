# Teachers API

## GET /api/v1/teachers

### Purpose
Lấy danh sách giáo viên có phân trang và tìm kiếm.

### Roles
`OWNER`

### Request
Query Parameters:
- `Page` (int): Trang hiện tại (mặc định 1)
- `PageSize` (int): Kích thước trang (mặc định 10)
- `Keyword` (string?): Từ khóa tìm kiếm (tên, mã giáo viên, số điện thoại, email)
- `Status` (string?): Lọc theo trạng thái (`Active`, `Inactive`)

### Response
```json
{
  "success": true,
  "message": "Success",
  "data": {
    "items": [
      {
        "userId": 1,
        "teacherCode": "GV001",
        "fullName": "Nguyễn Văn A",
        "phoneNumber": "0901234567",
        "email": "nva@gmail.com",
        "avatarUrl": null,
        "totalClasses": 5,
        "totalActiveStudents": 50,
        "status": "Active",
        "createdAt": "2024-01-01T00:00:00"
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

---

## GET /api/v1/teachers/{id}

### Purpose
Lấy thông tin chi tiết một giáo viên.

### Roles
`OWNER`

### Request
Path Parameters:
- `id` (int): UserId của giáo viên.

### Response
```json
{
  "success": true,
  "message": "Success",
  "data": {
    "userId": 1,
    "teacherCode": "GV001",
    "fullName": "Nguyễn Văn A",
    "phoneNumber": "0901234567",
    "email": "nva@gmail.com",
    "avatarUrl": null,
    "dateOfBirth": "1990-01-01",
    "gender": "Nam",
    "ethnicity": "Kinh",
    "religion": "Không",
    "identityNumber": "012345678910",
    "identityIssuedDate": "2015-01-01",
    "identityIssuedPlace": "Cục CS QLHC",
    "currentAddress": "Hà Nội",
    "permanentAddress": "Hà Nội",
    "hometown": "Hà Nội",
    "placeOfBirth": "Hà Nội",
    "status": "Active",
    "createdAt": "2024-01-01T00:00:00"
  }
}
```

---

## POST /api/v1/teachers

### Purpose
Thêm mới giáo viên.

### Roles
`OWNER`

### Request
```json
{
  "teacherCode": "GV002",
  "fullName": "Trần Thị B",
  "phoneNumber": "0987654321",
  "email": "ttb@gmail.com",
  "password": "Password123!",
  "dateOfBirth": "1992-05-15",
  "gender": "Nữ",
  "identityNumber": "012345678911",
  "isActive": true
}
```

### Response
```json
{
  "success": true,
  "message": "Đã thêm giáo viên thành công.",
  "data": {
    "userId": 2
  }
}
```

### Validation Rules
- TeacherCode: Bắt buộc, tối đa 50 ký tự.
- FullName: Bắt buộc, tối đa 100 ký tự.
- PhoneNumber: Bắt buộc, đúng định dạng số điện thoại.
- Password: Tối thiểu 6 ký tự, có ký tự đặc biệt, số.
- IdentityNumber: Bắt buộc, đúng định dạng.

### Error Cases
- Mã giáo viên đã tồn tại trong trung tâm.
- Số điện thoại đã được sử dụng.
- Email đã được sử dụng.
- CCCD đã được sử dụng.

---

## PUT /api/v1/teachers/{id}

### Purpose
Cập nhật thông tin giáo viên.

### Roles
`OWNER`

### Request
Path Parameters:
- `id` (int): UserId của giáo viên.

Body: Tương tự như `POST /api/v1/teachers` (trường `password` có thể bỏ qua).

### Response
```json
{
  "success": true,
  "message": "Đã cập nhật thông tin giáo viên.",
  "data": {
    "userId": 2
  }
}
```

---

## PATCH /api/v1/teachers/{id}/status

### Purpose
Cập nhật trạng thái giáo viên (Active/Inactive).

### Roles
`OWNER`

### Request
```json
{
  "status": "Inactive"
}
```

### Response
```json
{
  "success": true,
  "message": "Đã cập nhật trạng thái thành công.",
  "data": {
    "userId": 2
  }
}
```

---

## POST /api/v1/teachers/{id}/reset-password

### Purpose
Đặt lại mật khẩu giáo viên về mặc định (123456aA@).

### Roles
`OWNER`

### Request
Path Parameters:
- `id` (int): UserId của giáo viên.

### Response
```json
{
  "success": true,
  "message": "Mật khẩu đã được đặt lại thành công.",
  "data": {
    "newPassword": "..."
  }
}
```

---

## DELETE /api/v1/teachers/{id}

### Purpose
Xóa mềm giáo viên.

### Roles
`OWNER`

### Request
Path Parameters:
- `id` (int): UserId của giáo viên.

### Response
```json
{
  "success": true,
  "message": "Đã xóa giáo viên thành công.",
  "data": true
}
```

---

## POST /api/v1/teachers/{id}/avatar

### Purpose
Tải lên hoặc cập nhật ảnh đại diện của giáo viên.

### Roles
`OWNER`

### Request
`multipart/form-data`
- `file`: (IFormFile) Tệp ảnh (.jpg, .png, .webp). Tối đa 2MB.

### Response
```json
{
  "success": true,
  "message": "Cập nhật ảnh đại diện thành công.",
  "data": "/uploads/teachers/gv002-1234.png"
}
```

---

## DELETE /api/v1/teachers/{id}/avatar

### Purpose
Xóa ảnh đại diện của giáo viên.

### Roles
`OWNER`

### Request
Path Parameters:
- `id` (int): UserId của giáo viên.

### Response
```json
{
  "success": true,
  "message": "Đã xóa ảnh đại diện.",
  "data": true
}
```
