# API Bài Tập (Homework API)

Module này cung cấp các API cho phép giáo viên quản lý bài tập, giao bài tập mới cho lớp học với file PDF đính kèm (giới hạn tối đa **20MB**), chấm điểm bài nộp và cho phép phụ huynh nộp bài cho con.

---

## I. DÀNH CHO GIÁO VIÊN (TEACHER)

### 1. Lấy danh sách bài tập của giáo viên

#### Endpoint
```txt
GET /api/v1/teacher/homework
```

#### Purpose
Lấy danh sách tất cả các bài tập do giáo viên hiện tại quản lý và thông tin thống kê số lượng bài nộp (Đã nộp, Đã chấm, Chờ chấm, Tổng số học sinh).

#### Roles
```txt
TEACHER
```

#### Response
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
      "attachmentUrl": "/uploads/homeworks/a89f92a3-bb81-4235-862d-0a8a72bca531_debai.pdf",
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

---

### 2. Giao bài tập mới

#### Endpoint
```txt
POST /api/v1/teacher/homework
```

#### Purpose
Tạo mới một bài tập cho một buổi học của lớp do giáo viên phụ trách (có đính kèm file đề bài PDF).

#### Roles
```txt
TEACHER
```

#### Request DTO (`CreateHomeworkRequest` + `attachmentUrl`)
```json
{
  "lessonId": 5,
  "title": "Bài tập hàm số bậc hai",
  "description": "Làm bài tập trắc nghiệm trang 45-46 sách giáo khoa.",
  "dueDate": "2026-06-14T23:59:00",
  "attachmentUrl": "/uploads/homeworks/a89f92a3-bb81-4235-862d-0a8a72bca531_debai.pdf"
}
```

#### Validation Rules
- `lessonId`: Bắt buộc, ID buổi học hợp lệ và thuộc lớp học do giáo viên phụ trách.
- `title`: Bắt buộc, không quá 200 ký tự.
- `dueDate`: Bắt buộc, thời gian hết hạn nộp bài.
- `attachmentUrl`: Tùy chọn, đường dẫn tệp PDF đề bài.

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Giao bài tập thành công",
  "data": true
}
```

---

### 3. Tải lên tệp PDF đề bài (Giáo viên)

#### Endpoint
```txt
POST /api/v1/teacher/homework/upload
```

#### Purpose
Giáo viên tải lên file PDF đề bài trước khi tạo bài tập. Tối đa **20MB** và chỉ cho phép file đuôi `.pdf` (Magic bytes kiểm tra hợp lệ).

#### Roles
```txt
TEACHER
```

#### Request Body
- Dạng `multipart/form-data` chứa:
  - `file`: Tệp tin PDF đề bài.

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Tải lên file thành công",
  "data": {
    "fileUrl": "/uploads/homeworks/a89f92a3-bb81-4235-862d-0a8a72bca531_debai.pdf",
    "fileName": "debai.pdf"
  }
}
```

---

### 4. Lấy danh sách bài nộp và học sinh của lớp

#### Endpoint
```txt
GET /api/v1/teacher/homework/{id}/submissions
```

#### Purpose
Lấy danh sách tất cả học sinh trong lớp gắn với bài tập đó kèm trạng thái nộp bài (Submitted, NotSubmitted, Graded, Overdue), đường dẫn file bài làm (hình ảnh hoặc tài liệu khác), điểm số và nhận xét.

#### Roles
```txt
TEACHER
```

#### Response
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
      "submissionContent": "Gửi thầy bài làm của con.",
      "submissionFileUrl": "/uploads/homework_submissions/b90c12a8-aa82-4211-a89c_anh1.png",
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
      "submissionFileUrl": null,
      "status": "NotSubmitted",
      "score": null,
      "feedback": null,
      "submittedAtString": null
    }
  ]
}
```

---

### 5. Chấm điểm bài nộp

#### Endpoint
```txt
PUT /api/v1/teacher/homework/{id}/submissions/{studentId}/grade
```

#### Roles
```txt
TEACHER
```

#### Request DTO (`GradeSubmissionRequest`)
```json
{
  "score": 9.5,
  "feedback": "Bài làm tốt, trình bày sạch đẹp."
}
```

---

## II. DÀNH CHO PHỤ HUYNH (PARENT)

### 1. Lấy danh sách bài tập của các con

#### Endpoint
```txt
GET /api/v1/parent/homework
```

#### Purpose
Lấy danh sách bài tập từ các lớp học mà các con của phụ huynh này đang tham gia, kèm thông tin bài nộp hiện tại (nếu có).

#### Roles
```txt
PARENT
```

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Success",
  "data": [
    {
      "homeworkId": 1,
      "title": "Bài tập hàm số bậc hai",
      "description": "Làm bài tập trắc nghiệm trang 45-46 sách giáo khoa.",
      "className": "Toán 10A",
      "lessonTitle": "Buổi 5 - Tìm hiểu về Hàm số bậc hai",
      "attachmentUrl": "/uploads/homeworks/a89f92a3-bb81-4235-862d-0a8a72bca531_debai.pdf",
      "dueDate": "2026-06-14T23:59:00",
      "dueDateString": "14/06/2026 23:59",
      "studentId": 1,
      "studentName": "Phạm Quốc Bảo",
      "submissionId": null,
      "submissionContent": null,
      "submissionFileUrl": null,
      "submittedAtString": null,
      "status": "NotSubmitted", // Trạng thái: NotSubmitted, Submitted, Graded, Overdue
      "score": null,
      "feedback": null
    }
  ]
}
```

---

### 2. Tải lên tệp bài làm (Phụ huynh)

#### Endpoint
```txt
POST /api/v1/parent/homework/upload
```

#### Purpose
Phụ huynh tải lên tệp bài làm (hình ảnh hoặc tài liệu khác) của học sinh, giới hạn tối đa **20MB**.

#### Roles
```txt
PARENT
```

#### Request Body
- Dạng `multipart/form-data` chứa:
  - `file`: File bài làm (Ảnh hoặc tài liệu tài liệu).

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Tải lên file thành công",
  "data": {
    "fileUrl": "/uploads/homework_submissions/b90c12a8-aa82-4211-a89c_anh1.png",
    "fileName": "anh1.png"
  }
}
```

---

### 3. Nộp bài làm (Phụ huynh)

#### Endpoint
```txt
POST /api/v1/parent/homework/submit
```

#### Purpose
Phụ huynh thực hiện gửi thông tin nộp bài làm của học sinh. Nếu quá hạn nộp, API sẽ trả về lỗi không cho phép nộp bài.

#### Roles
```txt
PARENT
```

#### Request DTO (`SubmitHomeworkRequestDto`)
```json
{
  "homeworkId": 1,
  "studentId": 1,
  "submissionFileUrl": "/uploads/homework_submissions/b90c12a8-aa82-4211-a89c_anh1.png",
  "submissionContent": "Con gửi bài làm ạ"
}
```

#### Validation Rules
- `homeworkId`: Bắt buộc.
- `studentId`: Bắt buộc, phải thuộc quyền quản lý của phụ huynh.
- `submissionFileUrl`: Bắt buộc, đường dẫn file đã tải lên trước đó.

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Nộp bài tập thành công",
  "data": true
}
```

#### Error Cases
- **400 Bad Request:** Thời gian nộp quá hạn (`DueDate`), học sinh không thuộc quyền quản lý, hoặc bài tập không thuộc lớp học sinh đang học.
- **401 Unauthorized:** Phụ huynh chưa đăng nhập.
