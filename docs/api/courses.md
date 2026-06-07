# Course API

Quản lý môn học / khóa học.

## Các Endpoint

### 1. Lấy danh sách môn học

`GET /api/v1/courses`

#### Purpose
Lấy danh sách môn học có phân trang, hỗ trợ tìm kiếm và lọc.

#### Roles
`OWNER`

#### Request
Query Parameters:
- `keyword` (string, optional): Tìm theo mã môn hoặc tên môn.
- `status` (string, optional): Lọc theo trạng thái `Active` hoặc `Inactive`.
- `pageNumber` (int, default=1)
- `pageSize` (int, default=10)

#### Response
```json
{
  "success": true,
  "data": [
    {
      "courseId": 1,
      "courseCode": "IELTS01",
      "courseName": "IELTS Foundation",
      "description": "Lớp nền tảng",
      "totalSessions": 24,
      "tuitionFee": 5000000,
      "status": "Active",
      "classCount": 2,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": null
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

### 2. Chi tiết môn học

`GET /api/v1/courses/{id}`

#### Purpose
Lấy thông tin chi tiết một môn học theo ID.

#### Roles
`OWNER`

#### Request
Path Parameter: `id` (int)

#### Response
```json
{
  "success": true,
  "data": {
    "courseId": 1,
    "courseCode": "IELTS01",
    "courseName": "IELTS Foundation",
    "description": "Lớp nền tảng",
    "totalSessions": 24,
    "tuitionFee": 5000000,
    "status": "Active",
    "classCount": 2,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": null
  }
}
```

### 3. Tạo mới môn học

`POST /api/v1/courses`

#### Purpose
Thêm mới môn học vào trung tâm.

#### Roles
`OWNER`

#### Request
```json
{
  "courseCode": "IELTS02",
  "courseName": "IELTS Intensive",
  "description": "Lớp tăng tốc",
  "totalSessions": 36,
  "tuitionFee": 7000000,
  "status": "Active"
}
```

#### Response
```json
{
  "success": true,
  "message": "Thêm môn học thành công.",
  "data": {
    "courseId": 2,
    "courseCode": "IELTS02",
    "status": "Active"
  }
}
```

#### Validation Rules
- `courseCode`: Bắt buộc, tối đa 30 ký tự, không được trùng trong trung tâm.
- `courseName`: Bắt buộc, tối đa 150 ký tự, không được trùng trong trung tâm.
- `totalSessions`: 1 - 1000.
- `tuitionFee`: >= 0.

#### Error Cases
- Mã môn hoặc tên môn đã tồn tại.
- Validation failed.

### 4. Cập nhật môn học

`PUT /api/v1/courses/{id}`

#### Purpose
Sửa thông tin môn học.

#### Roles
`OWNER`

#### Request
Path Parameter: `id` (int)
```json
{
  "courseCode": "IELTS02",
  "courseName": "IELTS Intensive Pro",
  "description": "Cập nhật tên",
  "totalSessions": 40,
  "tuitionFee": 8000000,
  "status": "Active"
}
```

#### Response
```json
{
  "success": true,
  "message": "Cập nhật môn học thành công.",
  "data": {
    "courseId": 2,
    "courseCode": "IELTS02",
    "status": "Active"
  }
}
```

#### Error Cases
- Đổi sang trạng thái `Inactive` khi đang có lớp `Active`.
- Mã môn hoặc tên môn đã tồn tại.

### 5. Đổi trạng thái môn học

`PATCH /api/v1/courses/{id}/status`

#### Purpose
Bật/Tắt trạng thái hoạt động của môn học.

#### Roles
`OWNER`

#### Request
Path Parameter: `id` (int)
```json
{
  "status": "Inactive"
}
```

#### Response
```json
{
  "success": true,
  "message": "Đổi trạng thái môn học thành công.",
  "data": {
    "courseId": 2,
    "courseCode": "IELTS02",
    "status": "Inactive"
  }
}
```

#### Error Cases
- Không thể chuyển `Inactive` nếu còn lớp `Active`.
- Trạng thái không hợp lệ.

### 6. Xóa mềm môn học

`DELETE /api/v1/courses/{id}`

#### Purpose
Xóa mềm môn học (chuyển `IsDeleted = true`).

#### Roles
`OWNER`

#### Request
Path Parameter: `id` (int)

#### Response
```json
{
  "success": true,
  "message": "Xóa môn học thành công."
}
```

#### Error Cases
- Chỉ được xóa khi đang ở trạng thái `Inactive`.
- Không thể xóa khi đang có lớp `Active`.
