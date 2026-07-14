# API Điểm Danh (Attendance API)

Module này cung cấp các API cho phép giáo viên thực hiện điểm danh, xem lịch sử điểm danh và ghi nhận tình trạng chuyên cần của học sinh theo từng buổi học.

---

## 1. Lấy danh sách buổi học của lớp học

### Endpoint
```txt
GET /api/v1/teacher/attendance/lessons
```

### Query Parameters
- `classId`: Bắt buộc, ID lớp học muốn lấy danh sách buổi học.

### Purpose
Lấy danh sách tất cả các buổi học (Lesson) của một lớp học do giáo viên phụ trách để điền vào dropdown select điểm danh.

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
      "lessonId": 12,
      "lessonTitle": "Buổi 3 - Luyện tập Listening",
      "dateString": "07/06/2026"
    }
  ]
}
```

### Error Cases
- **401 Unauthorized:** Chưa đăng nhập.
- **403 Forbidden:** Giáo viên không giảng dạy lớp học được chọn.

---

## 2. Lấy bảng điểm danh của buổi học

### Endpoint
```txt
GET /api/v1/teacher/attendance
```

### Query Parameters
- `lessonId`: Bắt buộc, ID buổi học muốn lấy danh sách điểm danh.

### Purpose
Lấy danh sách tất cả học sinh đang học trong lớp thuộc buổi học đó kèm theo trạng thái điểm danh hiện tại (`Present`, `Late`, `Absent` hoặc `null` nếu chưa điểm danh) và ghi chú.

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
      "studentId": 1,
      "studentName": "Nguyễn Văn A",
      "studentCode": "STD00001",
      "status": "Present",
      "note": "Đi học đầy đủ"
    },
    {
      "studentId": 2,
      "studentName": "Trần Thị B",
      "studentCode": "STD00002",
      "status": null,
      "note": null
    }
  ]
}
```

---

## 3. Lưu bảng điểm danh của buổi học

### Endpoint
```txt
POST /api/v1/teacher/attendance/save
```

### Purpose
Thực hiện lưu mới hoặc cập nhật thông tin điểm danh, ghi chú cho cả lớp trong một buổi học cụ thể.

### Roles
```txt
TEACHER
```

### Request DTO (`SaveAttendanceRequest`)
```json
{
  "lessonId": 12,
  "attendances": [
    {
      "studentId": 1,
      "status": "Present",
      "note": "Có mặt đầy đủ"
    },
    {
      "studentId": 2,
      "status": "Absent",
      "note": "Nghỉ không phép"
    }
  ]
}
```

### Validation Rules
- `lessonId`: Bắt buộc, ID buổi học hợp lệ.
- `status`: Bắt buộc cho mỗi dòng học sinh, nhận một trong ba giá trị: `Present`, `Late`, `Absent`.
- `studentId`: Bắt buộc, ID học sinh hợp lệ và đang học trong lớp của buổi học đó.

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Lưu điểm danh thành công",
  "data": true
}
```

---

## 4. Lấy lịch sử điểm danh của lớp học

### Endpoint
```txt
GET /api/v1/teacher/attendance/history
```

### Query Parameters
- `classId`: Bắt buộc, ID lớp học muốn lấy lịch sử.

### Purpose
Lấy danh sách thống kê tình hình chuyên cần của tất cả các buổi học đã diễn ra trong lớp học đó (phục vụ bảng lịch sử chuyên cần).

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
      "lessonId": 12,
      "dateString": "07/06/2026",
      "className": "Toán 10A",
      "presentCount": 20,
      "absentCount": 2,
      "lateCount": 1,
      "attendanceRate": "91%"
    }
  ]
}
```
