# API quản lý lớp học

Các endpoint tạo lớp sử dụng JWT Bearer và yêu cầu role `OWNER`.

Base URL mới: `/api/v1/classes`

Route cũ `/api/classes` vẫn được giữ để không làm hỏng client hiện tại.

## Lấy dữ liệu tạo lớp

```http
GET /api/v1/classes/create-options?centerId=1
Authorization: Bearer {token}
```

Trả về mã lớp dự kiến, môn học, giáo viên, phòng học và ca học đang hoạt động thuộc trung tâm.

## Kiểm tra trùng lịch

```http
POST /api/v1/classes/check-conflicts
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "centerId": 1,
  "className": "Kids English A1 - Lớp 05",
  "courseId": 1,
  "teacherId": 1,
  "roomId": 1,
  "startDate": "2026-06-08",
  "totalSessions": 24,
  "schedules": [
    {
      "dayOfWeek": 1,
      "studyShiftId": 1,
      "startTime": "18:00:00",
      "endTime": "20:00:00"
    }
  ]
}
```

`dayOfWeek`: `1` là Thứ 2, ..., `7` là Chủ nhật.

Xung đột được kiểm tra theo từng buổi học đã lập, gồm giáo viên và phòng học.

## Tạo lớp

```http
POST /api/v1/classes
Authorization: Bearer {token}
Content-Type: application/json
```

Body giống endpoint kiểm tra trùng lịch.

Khi thành công, hệ thống thực hiện trong một transaction:

1. Kiểm tra quyền OWNER và dữ liệu thuộc trung tâm.
2. Kiểm tra lại xung đột lịch.
3. Sinh `ClassCode`.
4. Tạo `Classes`, `ClassSchedules` và toàn bộ `Lessons`.

## Lấy lịch cố định của lớp

```http
GET /api/v1/classes/{classId}/schedules
Authorization: Bearer {token}
```

Endpoint này hỗ trợ `OWNER`, `TEACHER`, `PARENT` theo phạm vi lớp mà tài khoản được phép truy cập.
# Quản lý học sinh trong lớp

Các endpoint dưới đây dùng JWT, role `OWNER` và luôn kiểm tra lớp/học sinh thuộc trung tâm mà OWNER quản lý.

## GET `/api/v1/classes/{classId}/students`

Trả về học sinh đang `Đang học` hoặc `Bảo lưu` trong lớp.

## GET `/api/v1/classes/{classId}/students/available?keyword=an`

Tìm tối đa 50 học sinh đang hoạt động, chưa nằm trong lớp. Tìm theo mã hoặc tên học sinh.

## POST `/api/v1/classes/{classId}/students`

Gán nhiều học sinh vào lớp trong một transaction.

```json
{
  "studentIds": [1, 2, 3],
  "note": "Xếp lớp đầu kỳ"
}
```

Validation:

- Lớp phải đang `Active`, chưa xóa và thuộc trung tâm của OWNER.
- Học sinh phải đang hoạt động, chưa xóa và thuộc cùng trung tâm.
- Tối đa 100 học sinh mỗi request.
- Enrollment mới có trạng thái `Đang học`.
- Enrollment `Đã nghỉ` được kích hoạt lại thành `Đang học`.
- Enrollment `Đang học` hoặc `Bảo lưu` bị từ chối vì vẫn thuộc lớp.
- Mọi thay đổi được lưu vào `EnrollmentHistories`.

## DELETE `/api/v1/classes/{classId}/students/{studentId}`

Gỡ học sinh khỏi lớp bằng cách chuyển Enrollment sang `Đã nghỉ`. Không xóa cứng và không xóa lịch sử điểm danh, điểm số hoặc hóa đơn.

Error cases:

- `401`: token không hợp lệ.
- `403/400`: không có quyền hoặc dữ liệu không thuộc trung tâm.
- `400`: lớp không hoạt động, học sinh không hợp lệ hoặc Enrollment đã `Đã nghỉ`.
