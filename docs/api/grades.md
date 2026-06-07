# API Sổ Điểm (Grades API)

Module này cung cấp các API cho phép giáo viên quản lý điểm số, nhận xét và đánh giá học lực của học sinh trong các lớp học được phân công giảng dạy.

---

## 1. Lấy danh sách bảng điểm của lớp học

### Endpoint
```txt
GET /api/v1/teacher/grades
```

### Query Parameters
- `classId`: Bắt buộc, ID lớp học muốn lấy bảng điểm.

### Purpose
Lấy danh sách tất cả học sinh đang học trong lớp cùng thông tin chi tiết các đầu điểm của từng học sinh (Kiểm tra 1, Kiểm tra 2, Giữa kỳ, Cuối kỳ) và điểm trung bình tạm tính.

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
      "scoreKT1": 8.5,
      "commentKT1": "Bài làm tốt",
      "scoreKT2": 9.0,
      "commentKT2": "Cải thiện tốt",
      "scoreMidterm": 8.0,
      "commentMidterm": null,
      "scoreFinal": null,
      "commentFinal": null,
      "averageScore": 8.5
    }
  ]
}
```

### Error Cases
- **400 Bad Request:** Thiếu tham số hoặc dữ liệu không hợp lệ.
- **401 Unauthorized:** Chưa đăng nhập hoặc phiên làm việc hết hạn.
- **403 Forbidden:** Người dùng không có quyền truy cập thông tin lớp học này (không phải giáo viên giảng dạy).

---

## 2. Lưu hoặc cập nhật điểm số học sinh

### Endpoint
```txt
POST /api/v1/teacher/grades/save
```

### Purpose
Lưu mới, cập nhật hoặc xóa (nếu gửi giá trị `null`) các đầu điểm của một học sinh trong lớp học do giáo viên phụ trách.

### Roles
```txt
TEACHER
```

### Request DTO (`SaveStudentGradesRequest`)
```json
{
  "classId": 12,
  "studentId": 1,
  "scoreKT1": 9.0,
  "commentKT1": "Tích cực phát biểu",
  "scoreKT2": null,
  "commentKT2": null,
  "scoreMidterm": 8.5,
  "commentMidterm": "Làm bài cẩn thận",
  "scoreFinal": 9.5,
  "commentFinal": "Xuất sắc"
}
```

### Validation Rules
- `classId`: Bắt buộc, ID lớp học hợp lệ.
- `studentId`: Bắt buộc, ID học sinh hợp lệ.
- `scoreKT1`, `scoreKT2`, `scoreMidterm`, `scoreFinal`: Nếu nhập, giá trị phải nằm trong khoảng từ `0.0` đến `10.0`. Nếu để trống (`null`), hệ thống tự động xóa bản ghi điểm tương ứng.

### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Cập nhật điểm số thành công",
  "data": true
}
```

### Error Cases
- **400 Bad Request:** Lỗi validation điểm số ngoài khoảng `[0, 10]`, hoặc học sinh không thuộc lớp học được chọn.
- **401 Unauthorized:** Chưa đăng nhập.
- **403 Forbidden:** Giáo viên không phụ trách giảng dạy lớp học này.
