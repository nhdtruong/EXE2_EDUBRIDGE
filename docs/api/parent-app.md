# Parent App API Documentation

Đây là tài liệu đặc tả các API dành riêng cho Mobile App của Phụ huynh. 
Tất cả các API dưới đây đều yêu cầu xác thực bằng **JWT Token** (được cấp từ `/api/auth/login`) và Role là **PARENT**.

Tất cả các response sẽ được bọc trong một chuẩn chung:
```json
{
  "success": true,
  "message": "Thành công",
  "data": { ... }
}
```

---

## 1. Dashboard
**Endpoint:** `GET /api/v1/parent/dashboard`
**Purpose:** Lấy dữ liệu tổng quan cho trang chủ của app phụ huynh (số dư nợ, số thông báo, lịch học sắp tới).
**Roles:** `PARENT`
**Response:** `ParentDashboardDto`

---

## 2. Children (Quản lý Học sinh)
**Endpoint:** `GET /api/v1/parent/children`
**Purpose:** Lấy danh sách các học sinh (con) thuộc quyền quản lý của phụ huynh.
**Roles:** `PARENT`
**Response:** `List<ParentChildOverviewDto>`

**Endpoint:** `GET /api/v1/parent/children/{studentId}`
**Purpose:** Lấy thông tin chi tiết và danh sách các lớp đang học của 1 học sinh cụ thể.
**Roles:** `PARENT`
**Response:** `ParentChildDetailDto`

---

## 3. Schedule (Lịch học)
**Endpoint:** `GET /api/v1/parent/schedule`
**Purpose:** Lấy lịch trình các buổi học của con.
**Query Params:**
- `studentId` (int, optional): Lọc theo ID của một học sinh cụ thể (nếu không truyền sẽ lấy lịch của tất cả các con).
- `fromDate` (DateOnly, optional): Từ ngày (YYYY-MM-DD)
- `toDate` (DateOnly, optional): Đến ngày (YYYY-MM-DD)
**Roles:** `PARENT`
**Response:** `List<ParentScheduleDto>`

---

## 4. Attendance (Điểm danh)
**Endpoint:** `GET /api/v1/parent/children/{studentId}/attendance`
**Purpose:** Lấy lịch sử điểm danh của một học sinh.
**Roles:** `PARENT`
**Response:** `List<ParentAttendanceDto>`

---

## 5. Academic (Học tập - Điểm số & Bài tập)
**Endpoint:** `GET /api/v1/parent/children/{studentId}/grades`
**Purpose:** Lấy danh sách điểm số của học sinh trong các kì thi/kiểm tra.
**Roles:** `PARENT`
**Response:** `List<ParentGradeDto>`

**Endpoint:** `GET /api/v1/parent/children/{studentId}/homeworks`
**Purpose:** Lấy thông tin bài tập về nhà, trạng thái nộp bài và nhận xét của giáo viên.
**Roles:** `PARENT`
**Response:** `List<ParentHomeworkDto>`

---

## 6. Finance (Tài chính - Hóa đơn)
**Endpoint:** `GET /api/v1/parent/invoices`
**Purpose:** Lấy danh sách hóa đơn học phí của phụ huynh.
**Query Params:**
- `studentId` (int, optional): Lọc hóa đơn theo một học sinh cụ thể.
**Roles:** `PARENT`
**Response:** `List<ParentInvoiceDto>`

---

## 7. Notifications (Thông báo)
**Endpoint:** `GET /api/v1/parent/notifications`
**Purpose:** Lấy danh sách 50 thông báo gần nhất của phụ huynh.
**Roles:** `PARENT`
**Response:** `List<ParentNotificationDto>`

**Endpoint:** `PUT /api/v1/parent/notifications/{id}/read`
**Purpose:** Đánh dấu một thông báo là đã đọc.
**Roles:** `PARENT`
**Response:** `bool`

---

## 8. Chat (Tin nhắn)
**Endpoint:** `GET /api/v1/parent/chat/conversations`
**Purpose:** Lấy danh bạ và các cuộc hội thoại gần nhất với giáo viên đang dạy các con.
**Roles:** `PARENT`
**Response:** `List<ParentChatConversationDto>`

**Endpoint:** `GET /api/v1/parent/chat/messages/{receiverId}`
**Purpose:** Lấy lịch sử 100 tin nhắn gần nhất giữa phụ huynh và một giáo viên (receiverId).
**Roles:** `PARENT`
**Response:** `List<ParentChatMessageDto>`

> **Lưu ý nghiệp vụ Chat:** Các API phía trên được dùng để lấy lịch sử. Quá trình gửi/nhận tin nhắn realtime được thực hiện thông qua kết nối **SignalR** tới `/chatHub`.

---

## Mobile V1 Additive APIs

Các API dưới đây được bổ sung riêng cho Parent App, không thay đổi contract API Web hiện có.

- `GET /api/v1/parent/children/{studentId}/lessons`: Nhật ký bài học read-only.
- `POST /api/v1/parent/chat/read?contactUserId={teacherUserId}`: Đánh dấu chat đã đọc.
- `POST /api/v1/parent/chat/upload`: Upload ảnh chat, multipart field `file`.
- `GET /api/v1/parent/children/{studentId}/leave-requests`: Danh sách yêu cầu xin nghỉ.
- `POST /api/v1/parent/children/{studentId}/leave-requests`: Gửi yêu cầu theo `lessonId`, hoặc theo `lessonDate` để chọn buổi đầu tiên trong ngày.
- `POST /api/v1/parent/devices`: Đăng ký Expo push token.

Hai API cuối yêu cầu chạy migration `edubridge_database/migration/20260615_001_parent_mobile.sql`.
