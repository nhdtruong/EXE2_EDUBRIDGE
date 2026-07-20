# API Tin Nhắn & Chat (Chat API)

Module này cung cấp các API cho phép cả giáo viên và phụ huynh quản lý các cuộc hội thoại, xem lịch sử tin nhắn, đánh dấu đã đọc và gửi các tài liệu/hình ảnh/video đính kèm (giới hạn tối đa **10MB**).

---

## I. DÀNH CHO GIÁO VIÊN (TEACHER)

### 1. Lấy danh sách cuộc hội thoại của giáo viên

#### Endpoint
```txt
GET /api/v1/teacher/chat/conversations
```

#### Purpose
Lấy danh sách tất cả các phụ huynh có con đang học trong các lớp do giáo viên hiện tại giảng dạy, kèm theo preview tin nhắn cuối cùng và số lượng tin nhắn chưa đọc từ phụ huynh đó.

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
      "parentUserId": 3,
      "parentName": "Phạm Thị Lan",
      "studentNames": "Phạm Quốc Bảo, Phạm Khánh An",
      "lastMessage": "Con em có thể học bù vào thứ 7 được không ạ?",
      "lastMessageTime": "07/06/2026 14:30",
      "unreadCount": 1
    }
  ]
}
```

#### Error Cases
- **401 Unauthorized:** Giáo viên chưa đăng nhập.

---

### 2. Lấy lịch sử tin nhắn của giáo viên

#### Endpoint
```txt
GET /api/v1/teacher/chat/history
```

#### Query Parameters
- `contactUserId`: Bắt buộc, ID tài khoản người dùng của đối tác chat (Phụ huynh).

#### Purpose
Tải toàn bộ lịch sử tin nhắn gửi và nhận giữa giáo viên hiện tại và phụ huynh được chọn, sắp xếp theo thời gian tăng dần.

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
      "messageId": 14,
      "senderUserId": 3,
      "receiverUserId": 2,
      "content": "Xin chào cô, con em học bài này hơi khó. Có thể giải thích thêm không ạ?",
      "sentAtString": "07/06/2026 14:30",
      "isRead": true,
      "isOutgoing": false
    },
    {
      "messageId": 15,
      "senderUserId": 2,
      "receiverUserId": 3,
      "content": "Chào phụ huynh. Cô sẽ gửi thêm tài liệu và video bài giảng cho em ạ. Phụ huynh có thể cho em xem lại.",
      "sentAtString": "07/06/2026 14:35",
      "isRead": true,
      "isOutgoing": true
    }
  ]
}
```

#### Error Cases
- **401 Unauthorized:** Giáo viên chưa đăng nhập.

---

### 3. Đánh dấu đã đọc tin nhắn (Giáo viên)

#### Endpoint
```txt
POST /api/v1/teacher/chat/read
```

#### Query Parameters
- `contactUserId`: Bắt buộc, ID tài khoản người dùng của đối tác chat (Phụ huynh).

#### Purpose
Đánh dấu tất cả các tin nhắn chưa đọc mà phụ huynh này gửi cho giáo viên hiện tại thành đã đọc (`IsRead = true`).

#### Roles
```txt
TEACHER
```

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Đã đánh dấu đã đọc",
  "data": true
}
```

---

### 4. Tải lên file đính kèm chat (Giáo viên)

#### Endpoint
```txt
POST /api/v1/teacher/chat/upload
```

#### Request Body
- Dạng `multipart/form-data` chứa:
  - `file`: File cần upload (tài liệu, hình ảnh hoặc video, tối đa **10MB**).

#### Purpose
Tải lên tài liệu hoặc hình ảnh đính kèm trong cửa sổ chat, lưu trữ trên máy chủ và trả về đường dẫn file để phát đi qua SignalR.

#### Roles
```txt
TEACHER
```

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Tải lên file thành công",
  "data": {
    "fileUrl": "/uploads/chat/75ea9c5f-cf92-491b-85ef-13f59e917d23_tailieu.pdf",
    "fileName": "tailieu.pdf"
  }
}
```

#### Error Cases
- **400 Bad Request:** File trống, file vượt quá **10MB**, hoặc file có định dạng bị chặn (.exe, .bat, .cmd, .sh, .php, .js...).
- **401 Unauthorized:** Giáo viên chưa đăng nhập.

---

## II. DÀNH CHO PHỤ HUYNH (PARENT)

### 1. Lấy danh sách giáo viên đang dạy các con

#### Endpoint
```txt
GET /api/v1/parent/chat/conversations
```

#### Purpose
Lấy danh sách tất cả các giáo viên đang dạy các con của phụ huynh này, kèm theo preview tin nhắn cuối cùng và số lượng tin nhắn chưa đọc từ giáo viên đó.

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
      "parentUserId": 2, // ID của giáo viên (tương thích với ParentUserId trên client UI)
      "parentName": "Nguyễn Thị Mai", // Tên giáo viên
      "studentNames": "Phạm Quốc Bảo", // Tên các con học lớp giáo viên này
      "lastMessage": "Hôm nay em Bảo học bài rất tốt nhé phụ huynh.",
      "lastMessageTime": "07/06/2026 17:00",
      "unreadCount": 0
    }
  ]
}
```

#### Error Cases
- **401 Unauthorized:** Phụ huynh chưa đăng nhập.

---

### 2. Lấy lịch sử tin nhắn của phụ huynh

#### Endpoint
```txt
GET /api/v1/parent/chat/history
```

#### Query Parameters
- `contactUserId`: Bắt buộc, ID tài khoản người dùng của đối tác chat (Giáo viên).

#### Purpose
Tải toàn bộ lịch sử tin nhắn gửi và nhận giữa phụ huynh hiện tại và giáo viên được chọn, sắp xếp theo thời gian tăng dần.

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
      "messageId": 25,
      "senderUserId": 2, // Giáo viên gửi
      "receiverUserId": 3, // Phụ huynh nhận
      "content": "Chào phụ huynh, cô gửi kết quả kiểm tra định kỳ của con.",
      "sentAtString": "07/06/2026 17:00",
      "isRead": true,
      "isOutgoing": false
    }
  ]
}
```

---

### 3. Đánh dấu đã đọc tin nhắn (Phụ huynh)

#### Endpoint
```txt
POST /api/v1/parent/chat/read
```

#### Query Parameters
- `contactUserId`: Bắt buộc, ID tài khoản người dùng của đối tác chat (Giáo viên).

#### Purpose
Đánh dấu tất cả các tin nhắn chưa đọc mà giáo viên này gửi cho phụ huynh hiện tại thành đã đọc (`IsRead = true`).

#### Roles
```txt
PARENT
```

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Đã đánh dấu đã đọc",
  "data": true
}
```

---

### 4. Tải lên file đính kèm chat (Phụ huynh)

#### Endpoint
```txt
POST /api/v1/parent/chat/upload
```

#### Request Body
- Dạng `multipart/form-data` chứa:
  - `file`: File cần upload (tài liệu, hình ảnh hoặc video, tối đa **10MB**).

#### Purpose
Tải lên tài liệu hoặc hình ảnh đính kèm trong cửa sổ chat cho phụ huynh, lưu trữ trên máy chủ và trả về đường dẫn file để phát đi qua SignalR.

#### Roles
```txt
PARENT
```

#### Response
- **Mã phản hồi:** `200 OK`
```json
{
  "success": true,
  "message": "Tải lên file thành công",
  "data": {
    "fileUrl": "/uploads/chat/e4396b27-023a-4467-9bf4-da7f91cc5a31_baitapvenha.jpg",
    "fileName": "baitapvenha.jpg"
  }
}
```

#### Error Cases
- **400 Bad Request:** File trống, file vượt quá **10MB**, hoặc file có định dạng bị chặn (.exe, .bat, .cmd, .sh, .php, .js...).
- **401 Unauthorized:** Phụ huynh chưa đăng nhập.
