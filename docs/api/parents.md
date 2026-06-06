# Parent Management API

Các endpoint quản lý tài khoản phụ huynh dùng JWT và chỉ cho phép role `OWNER`.
Mọi dữ liệu được giới hạn theo trung tâm đang hoạt động của OWNER.

## GET /api/v1/parents

Lấy danh sách phụ huynh có phân trang.

Query: `name`, `email`, `phoneNumber`, `status`, `hasChildren`, `page`, `pageSize`.

Validation:
- `status`: `Active` hoặc `Inactive`.
- `pageSize`: `10`, `20`, `50`, `100`, `200`, `500`.
- `hasChildren`: `true` hoặc `false`.

## GET /api/v1/parents/{id}

Lấy thông tin phụ huynh và danh sách học sinh thuộc trung tâm hiện tại.

Error cases: phụ huynh không thuộc trung tâm, tài khoản đã xóa.

## POST /api/v1/parents

Tạo hoặc gắn tài khoản phụ huynh global vào trung tâm.

Request:
```json
{
  "fullName": "Nguyễn Văn A",
  "phoneNumber": "0987654321",
  "email": "parent@example.com",
  "dateOfBirth": "1985-05-20",
  "gender": "Nam",
  "identityNumber": "012345678901",
  "identityIssuedDate": "2015-05-20",
  "identityIssuedPlace": "Cục Cảnh sát QLHC về TTXH",
  "ethnicity": "Kinh",
  "religion": "Không",
  "currentAddress": "Hà Nội",
  "permanentAddress": "Hải Phòng",
  "hometown": "Hải Phòng",
  "placeOfBirth": "Hải Phòng",
  "status": "Active"
}
```

Business rules:
- Không tạo `Users` trùng email hoặc số điện thoại.
- Từ chối yêu cầu nếu số điện thoại và email đang thuộc hai tài khoản global khác nhau.
- User đã tồn tại phải có role `PARENT`.
- User đã thuộc trung tâm hiện tại không được gắn trùng.
- Mật khẩu mặc định của tài khoản mới là `edubridge2026`.

## PUT /api/v1/parents/{id}

Cập nhật thông tin phụ huynh và trạng thái membership tại trung tâm.

## PATCH /api/v1/parents/{id}/status

Request:
```json
{ "status": "Inactive" }
```

Ngừng membership không xóa học sinh liên kết và không khóa tài khoản nếu phụ huynh còn membership active tại trung tâm khác.

## POST /api/v1/parents/{id}/children/{studentId}

Đặt phụ huynh làm phụ huynh chính của học sinh.

Validation:
- Phụ huynh và học sinh phải cùng trung tâm.
- Database hiện chỉ hỗ trợ một phụ huynh chính trên mỗi học sinh.

Notes:
- Thao tác này thay đổi phụ huynh chính hiện tại của học sinh.
- Chưa hỗ trợ hủy liên kết hoàn toàn vì `Students.ParentUserId` đang là khóa ngoại bắt buộc.
- Muốn hỗ trợ nhiều phụ huynh hoặc hủy liên kết cần xác nhận thay đổi schema trước.

## GET /api/v1/parents/{id}/children/linkable

Lấy tối đa 50 học sinh cùng trung tâm có thể đổi phụ huynh chính sang phụ huynh đang xem.

## POST /api/v1/parents/{id}/reset-password

Cấp mật khẩu tạm thời ngẫu nhiên gồm 6 chữ số. Mật khẩu chỉ được trả về một lần trong response và không được ghi log.

## Response

```json
{
  "success": true,
  "message": "Success",
  "data": {}
}
```

API không trả về `PasswordHash` hoặc trường xác thực nhạy cảm.
