# Dashboard API

Tài liệu API cho chức năng thống kê và tổng quan (Dashboard) dành cho Chủ trung tâm (OWNER).

## Endpoints

### 1. Lấy thông tin tổng quan Dashboard
Lấy toàn bộ thông tin thống kê số lượng học sinh, doanh thu, biểu đồ dành cho trang chủ.

- **Endpoint**: `GET /api/v1/dashboard/summary`
- **Method**: `GET`
- **Roles**: `OWNER`

#### Request
*(Không có query parameters)*

#### Response (Thành công)
```json
{
  "success": true,
  "message": "Lấy thông tin tổng quan thành công.",
  "data": {
    "centerName": "IELTS Center",
    "totalStudents": 150,
    "studentChangeText": "+12 so với tháng trước",
    "activeClasses": 20,
    "classChangeText": "0 so với tháng trước",
    "monthlyRevenue": 45000000.0,
    "revenueChangeText": "+5.5% so với tháng trước",
    "weeklyAttendanceRate": 95.5,
    "attendanceChangeText": "+2.1% so với tuần trước",
    "latestClasses": [
      {
        "className": "IELTS Foundation K10",
        "teacherName": "Nguyen Van A",
        "totalStudents": 15
      }
    ],
    "importantNotifications": [
      {
        "title": "Học sinh mới đăng ký",
        "content": "Học sinh Trần Văn B vừa đăng ký vào lớp K10",
        "levelCssClass": "text-blue-500"
      }
    ],
    "revenueChart": {
      "labels": ["Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6"],
      "values": [30000000, 35000000, 32000000, 40000000, 42000000, 45000000]
    },
    "attendanceChart": {
      "labels": ["Thứ 2, 01/06", "Thứ 3, 02/06", "Thứ 4, 03/06", "Thứ 5, 04/06", "Thứ 6, 05/06", "Thứ 7, 06/06", "CN, 07/06"],
      "presentValues": [50, 52, 48, 55, 50, 60, 45],
      "absentValues": [2, 1, 3, 0, 1, 0, 5]
    }
  }
}
```

#### Error Cases
- **401 Unauthorized**: Token không hợp lệ hoặc thiếu token.
- **403 Forbidden**: Role không phải là `OWNER`.
- **400 Bad Request**:
```json
{
  "success": false,
  "message": "Không tìm thấy trung tâm hoạt động.",
  "data": null,
  "errors": {}
}
```

## Lưu ý nghiệp vụ
- API tự động xác định trung tâm của OWNER dựa trên `UserId` giải mã từ JWT token.
- API lấy dữ liệu 6 tháng gần nhất cho biểu đồ doanh thu và 7 ngày gần nhất cho biểu đồ điểm danh.
- Thời gian tính toán tự động lấy theo TimeZone `Asia/Ho_Chi_Minh` để đảm bảo báo cáo ngày tháng chính xác.
