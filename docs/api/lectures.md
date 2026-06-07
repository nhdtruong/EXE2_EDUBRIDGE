# API Bài Giảng (Lectures API)

Module này cung cấp các API cho phép giáo viên quản lý tiến độ giảng dạy, lưu và cập nhật ghi chú bài học của các lớp học được phân công phụ trách.

---

## 1. Lấy thông tin bài giảng và tiến độ lớp học

### Endpoint
```txt
GET /api/v1/teacher/lectures
```

### Purpose
Lấy danh sách tiến độ các lớp học giáo viên đang dạy và lịch sử 20 bài giảng/ghi chú bài giảng gần nhất.

### Roles
```txt
TEACHER
```

### Request
Không yêu cầu Request Body (Lấy ID giáo viên từ thông tin đăng nhập).

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Success",
  "data": {
    "classProgresses": [
      {
        "classId": 12,
        "className": "IELTS Foundation K06",
        "completedLessons": 5,
        "totalLessons": 20,
        "percentComplete": 25
      }
    ],
    "lectureHistories": [
      {
        "lessonId": 32,
        "classId": 12,
        "dateString": "07/06/2026",
        "className": "IELTS Foundation K06",
        "topic": "Học âm /e/ và /æ/",
        "content": "Học sinh luyện phát âm tốt, giao bài tập trang 12.",
        "status": "Completed",
        "createdAt": "2026-06-07T12:00:00"
      }
    ]
  }
}
```

### Error Cases
- **401 Unauthorized:** Không tìm thấy thông tin đăng nhập hoặc phiên làm việc hết hạn.
```json
{
  "success": false,
  "message": "Không tìm thấy thông tin đăng nhập",
  "data": null
}
```
- **403 Forbidden:** Người dùng không có vai trò Giáo viên.

---

## 2. Thêm ghi chú bài giảng mới

### Endpoint
```txt
POST /api/v1/teacher/lectures/note
```

### Purpose
Tạo mới một ghi chú bài giảng (buổi học) cho lớp học giáo viên phụ trách.

### Roles
```txt
TEACHER
```

### Request DTO (`AddLectureNoteRequest`)
```json
{
  "classId": 12,
  "topic": "Học âm /e/ và /æ/",
  "content": "Học sinh luyện phát âm tốt, giao bài tập trang 12.",
  "status": "Completed"
}
```

### Validation Rules
- `classId`: Bắt buộc, phải là ID lớp học đang hoạt động (`Active`).
- `topic`: Bắt buộc, chuỗi không quá 200 ký tự.
- `content`: Bắt buộc, chuỗi văn bản ghi chú.
- `status`: Bắt buộc, một trong các giá trị: `Scheduled`, `Completed`.

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Thêm ghi chú thành công",
  "data": true
}
```

### Error Cases
- **400 BadRequest:** Thiếu các trường bắt buộc hoặc dữ liệu không hợp lệ.
```json
{
  "success": false,
  "message": "Không thể thêm ghi chú bài giảng",
  "data": false
}
```
- **401 Unauthorized:** Chưa đăng nhập.

---

## 3. Cập nhật thông tin bài giảng

### Endpoint
```txt
PUT /api/v1/teacher/lectures/{id}
```

### Purpose
Cập nhật thông tin chủ đề, nội dung ghi chú và trạng thái của một bài giảng đã tạo.

### Roles
```txt
TEACHER
```

### Request DTO (`EditLectureNoteRequest`)
```json
{
  "topic": "Luyện nói chủ đề Family",
  "content": "Cả lớp tích cực phát biểu, ôn tập lại từ vựng cũ.",
  "status": "Completed"
}
```

### Validation Rules
- `topic`: Bắt buộc, chuỗi không quá 200 ký tự.
- `content`: Bắt buộc, chuỗi văn bản ghi chú.
- `status`: Bắt buộc, phải thuộc một trong các giá trị: `Scheduled`, `Completed`, `Cancelled`, `Rescheduled`.

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Cập nhật ghi chú thành công",
  "data": true
}
```

### Error Cases
- **400 BadRequest:** Trạng thái không hợp lệ hoặc không thể cập nhật ghi chú.
```json
{
  "success": false,
  "message": "Không thể cập nhật ghi chú bài giảng",
  "data": false
}
```
- **401 Unauthorized:** Chưa đăng nhập.

