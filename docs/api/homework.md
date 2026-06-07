# API Bài Tập (Homework API)

Module này cung cấp các API cho phép giáo viên quản lý bài tập, giao bài tập mới cho lớp học và chấm điểm bài nộp của học sinh.

---

## 1. Lấy danh sách bài tập

### Endpoint
```txt
GET /api/v1/teacher/homework
```

### Purpose
Lấy danh sách tất cả các bài tập do giáo viên hiện tại quản lý và thông tin thống kê số lượng bài nộp (Đã nộp, Đã chấm, Chờ chấm, Tổng số học sinh).

### Roles
```txt
TEACHER
```

### Request
Không yêu cầu Request Body (Lấy ID giáo viên đăng nhập qua Claims).

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Success",
  "data": [
    {
      "homeworkId": 1,
      "lessonId": 5,
      "className": "Toán 10A",
      "title": "Bài tập hàm số bậc hai",
      "description": "Làm bài tập trắc nghiệm trang 45-46 sách giáo khoa.",
      "createdAtString": "07/06/2026 08:00",
      "dueDateString": "14/06/2026 23:59",
      "submittedCount": 15,
      "totalStudents": 20,
      "gradedCount": 10,
      "pendingCount": 5
    }
  ]
}
```

### Error Cases
- **401 Unauthorized:** Chưa đăng nhập hoặc phiên làm việc hết hạn.
```json
{
  "success": false,
  "message": "Không tìm thấy thông tin đăng nhập",
  "data": null
}
```
- **403 Forbidden:** Người dùng không có vai trò Giáo viên.

---

## 2. Giao bài tập mới

### Endpoint
```txt
POST /api/v1/teacher/homework
```

### Purpose
Tạo mới một bài tập cho một buổi học của lớp do giáo viên phụ trách.

### Roles
```txt
TEACHER
```

### Request DTO (`CreateHomeworkRequest`)
```json
{
  "lessonId": 5,
  "title": "Bài tập hàm số bậc hai",
  "description": "Làm bài tập trắc nghiệm trang 45-46 sách giáo khoa.",
  "dueDate": "2026-06-14T23:59:00"
}
```

### Validation Rules
- `lessonId`: Bắt buộc, ID buổi học hợp lệ và thuộc lớp học do giáo viên phụ trách.
- `title`: Bắt buộc, không quá 200 ký tự.
- `dueDate`: Bắt buộc, thời gian hết hạn nộp bài.

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Giao bài tập thành công",
  "data": true
}
```

### Error Cases
- **400 Bad Request:** Dữ liệu đầu vào không hợp lệ hoặc buổi học không thuộc lớp học của giáo viên.
```json
{
  "success": false,
  "message": "Dữ liệu không hợp lệ",
  "data": false
}
```
- **401 Unauthorized:** Chưa đăng nhập.

---

## 3. Lấy danh sách bài nộp và học sinh

### Endpoint
```txt
GET /api/v1/teacher/homework/{id}/submissions
```

### Purpose
Lấy danh sách tất cả học sinh trong lớp gắn với bài tập đó kèm trạng thái nộp bài, điểm số và nhận xét (phục vụ màn hình chấm điểm).

### Roles
```txt
TEACHER
```

### Request
Không yêu cầu Request Body. Tham số `id` là ID bài tập trên URL path.

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
      "submissionId": 10,
      "submissionContent": "Em xin nộp link bài làm: github.com/...",
      "status": "Submitted",
      "score": null,
      "feedback": null,
      "submittedAtString": "07/06/2026 10:15"
    },
    {
      "studentId": 2,
      "studentName": "Trần Thị B",
      "studentCode": "STD00002",
      "submissionId": null,
      "submissionContent": null,
      "status": "NotSubmitted",
      "score": null,
      "feedback": null,
      "submittedAtString": null
    }
  ]
}
```

### Error Cases
- **401 Unauthorized:** Chưa đăng nhập.
- **404 Not Found:** Bài tập không tồn tại hoặc không thuộc lớp học do giáo viên giảng dạy.

---

## 4. Chấm điểm bài nộp

### Endpoint
```txt
PUT /api/v1/teacher/homework/{id}/submissions/{studentId}/grade
```

### Purpose
Thực hiện chấm điểm và ghi nhận nhận xét/phản hồi cho bài tập của một học sinh.

### Roles
```txt
TEACHER
```

### Request DTO (`GradeSubmissionRequest`)
```json
{
  "score": 9.5,
  "feedback": "Bài làm tốt, trình bày sạch đẹp."
}
```

### Validation Rules
- `score`: Bắt buộc, giá trị thập phân nằm trong khoảng `[0, 10]`.
- `feedback`: Chuỗi nhận xét (tùy chọn).

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Chấm điểm thành công",
  "data": true
}
```

### Error Cases
- **400 Bad Request:** Điểm số nằm ngoài phạm vi cho phép hoặc thông tin không hợp lệ.
- **401 Unauthorized:** Chưa đăng nhập.
- **404 Not Found:** Bài tập hoặc bài nộp của học sinh không hợp lệ.

---

## 5. Lấy danh sách bài học của lớp học làm tùy chọn giao bài tập

### Endpoint
```txt
GET /api/v1/teacher/homework/lessons
```

### Query Parameters
- `classId`: Bắt buộc, ID lớp học.

### Purpose
Lấy danh sách các buổi học của một lớp học để điền vào dropdown list khi giáo viên chuẩn bị giao bài tập cho lớp đó.

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
      "lessonId": 5,
      "lessonTitle": "Buổi 5 - Tìm hiểu về Hàm số bậc hai",
      "dateString": "07/06/2026"
    }
  ]
}
```

### Error Cases
- **401 Unauthorized:** Chưa đăng nhập.
- **404 Not Found:** Lớp học không hợp lệ hoặc giáo viên không phụ trách lớp này.
