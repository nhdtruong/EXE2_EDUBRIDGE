\# EduBridge AI Agent Context



\## Project Overview



EduBridge là hệ thống quản lý trung tâm giáo dục.



\### Technology Stack



\* ASP.NET Core Razor Pages (.NET 8)

\* C#

\* Entity Framework Core

\* SQL Server

\* Database First

\* Cookie Authentication

\* SignalR (Realtime Chat)

\* Bootstrap 5

\* Visual Studio 2022



\---



\## Language



Luôn trả lời bằng tiếng Việt.



\---



\## Coding Standards



\### General Rules



\* Ưu tiên code sạch, dễ bảo trì.

\* Không tạo code thừa.

\* Không refactor ngoài phạm vi yêu cầu.

\* Không tự ý đổi tên file, class, property hoặc table.

\* Không tự ý tạo migration nếu chưa được yêu cầu.

\* Không tự ý thay đổi cấu trúc database.

\* Không xoá dữ liệu hoặc thực hiện UPDATE/DELETE trên database khi chưa được yêu cầu rõ ràng.



\---



\## Project Architecture



\### Authentication



Hệ thống sử dụng Cookie Authentication.



Đăng nhập bằng:



\* Email

\* Hoặc số điện thoại



\### Roles



Chỉ có 3 vai trò chính:



\#### OWNER



Chủ trung tâm



\#### TEACHER



Giáo viên



\#### PARENT



Phụ huynh



\---



\## Database Rules



\### Database



Tên database:



EduBridgeDB



\### ORM



Entity Framework Core Database First



Khi cần thay đổi model:



1\. Kiểm tra database trước.

2\. Kiểm tra DbContext.

3\. Kiểm tra Entity Model.

4\. Kiểm tra Razor Page liên quan.



Không được suy đoán cấu trúc bảng.



\---



\## Development Workflow



Khi nhận yêu cầu mới:



\### Bước 1



Phân tích chức năng.



\### Bước 2



Xác định các file liên quan:



\* Razor Page (.cshtml)

\* PageModel (.cshtml.cs)

\* DbContext

\* Entity Model

\* Service

\* Repository



\### Bước 3



Đọc toàn bộ luồng hiện tại.



\### Bước 4



Đề xuất phương án.



\### Bước 5



Mới tiến hành sửa code.



\---



\## Response Format



Khi hỗ trợ code:



Luôn trình bày:



\### File cần sửa



Ví dụ:



Pages/Classes/Create.cshtml



\### Lý do sửa



Giải thích ngắn gọn.



\### Code cần thay



Hiển thị đầy đủ code.



\### Tác động



Liệt kê những file hoặc chức năng bị ảnh hưởng.



\---



\## UI/UX Rules



\* Ưu tiên giao diện quản trị chuyên nghiệp.

\* Responsive.

\* Sử dụng Bootstrap 5.

\* Tránh popup lồng popup.

\* Ưu tiên modal cho CRUD đơn giản.

\* Ưu tiên page riêng cho quy trình nhiều bước.

\* Form validation phải có cả Client và Server side.



\---



\## Class Management Rules



\### Class Status



\* UPCOMING

\* ACTIVE

\* COMPLETED

\* CANCELLED



\### Teacher Status



\* ACTIVE

\* INACTIVE



\---



\## Parent Management Rules



Mỗi phụ huynh có thể quản lý nhiều học sinh.



Mỗi học sinh chỉ thuộc một phụ huynh chính.



\---



\## Student Management Rules



StudentCode được hệ thống tự sinh.



Không yêu cầu email hoặc số điện thoại đối với học sinh nhỏ tuổi.



\---



\## Chat Rules



SignalR Realtime Chat.



Không polling.



Tin nhắn phải được lưu database.



\---



\## Attendance Rules



Điểm danh theo từng buổi học.



Mỗi bản ghi điểm danh phải gắn với:



\* Student

\* Lesson

\* Attendance Status



\---



\## AI Agent Instructions



Khi bắt đầu phân tích dự án:



1\. Đọc solution.

2\. Đọc appsettings.json.

3\. Đọc DbContext.

4\. Đọc Entity Models.

5\. Đọc Authentication configuration.

6\. Đọc toàn bộ Razor Pages.

7\. Đọc database schema qua MCP SQL Server.

8\. Đối chiếu Code ↔ Database.

9\. Báo cáo các vấn đề phát hiện được.

10\. Chỉ sửa code khi được yêu cầu.



\---



\## MCP SQL Server



Nếu MCP SQL Server khả dụng:



Ưu tiên đọc:



\* Tables

\* Columns

\* Foreign Keys

\* Indexes

\* Constraints

\* Sample Data



Trước khi đưa ra bất kỳ đề xuất nào liên quan đến database.



\---

\---



\## API-First Architecture Rules



EduBridge chắc chắn sẽ có Mobile App và có thể có thêm Frontend App hoặc External Integration trong tương lai.



Vì vậy, mọi chức năng mới phải được thiết kế theo hướng API-first.



\### Mandatory API Requirement



Không được chỉ code Razor Pages.



Mỗi chức năng nghiệp vụ quan trọng phải có khả năng được gọi thông qua API để Mobile App có thể sử dụng.



Các module bắt buộc phải hỗ trợ API:



\* Subject Management

\* Teacher Management

\* Parent Management

\* Student Management

\* Class Management

\* Room Management

\* Shift Management

\* Attendance

\* Notification

\* Chat



\---



\## Required Architecture



Khi phát triển chức năng mới, phải ưu tiên kiến trúc theo thứ tự:



```txt

Database

↓

Repository / DbContext

↓

Service Layer

↓

API Controller

↓

Razor Pages / Mobile App

```



Razor Pages không được chứa business logic chính.



Razor Pages chỉ nên xử lý:



\* Hiển thị UI

\* Binding form

\* Validate cơ bản phía UI

\* Gọi Service

\* Điều hướng trang



Business logic bắt buộc phải đặt trong Service Layer để API và Razor Pages có thể dùng chung.



\---



\## Business Logic Reuse Rule



Nếu một nghiệp vụ có thể được dùng bởi cả Web App và Mobile App, bắt buộc đưa vào Service.



Ví dụ:



\* Tạo lớp học

\* Cập nhật lớp học

\* Check trùng lịch giáo viên

\* Check trùng phòng học

\* Sinh buổi học

\* Gán học sinh vào lớp

\* Điểm danh

\* Gửi thông báo

\* Gửi tin nhắn

\* Xác thực quyền truy cập



Không được duplicate logic giữa:



\* Razor PageModel

\* API Controller

\* SignalR Hub



\---



\## API Controller Rules



Khi tạo API mới, cần dùng API Controller riêng.



Endpoint nên theo chuẩn RESTful và có versioning:



```txt

/api/v1/classes

/api/v1/subjects

/api/v1/teachers

/api/v1/students

/api/v1/attendance

```



Không đặt tên endpoint mơ hồ như:



```txt

/api/getData

/api/manage

/api/process

```



Ví dụ endpoint chuẩn:



```txt

GET    /api/v1/classes

GET    /api/v1/classes/{id}

POST   /api/v1/classes

PUT    /api/v1/classes/{id}

DELETE /api/v1/classes/{id}

```



\---



\## DTO and Response Rules



API không nên trả trực tiếp Entity nếu không cần thiết.



Bắt buộc ưu tiên dùng:



\* Request DTO

\* Response DTO

\* ViewModel nếu dùng cho Razor Pages

\* ApiResponse wrapper nếu cần response thống nhất



Response thành công nên theo dạng:



```json

{

&#x20; "success": true,

&#x20; "message": "Success",

&#x20; "data": {}

}

```



Response lỗi nên theo dạng:



```json

{

&#x20; "success": false,

&#x20; "message": "Validation failed",

&#x20; "errors": \[]

}

```



\---



\## API Authentication and Authorization



Web hiện tại dùng Cookie Authentication.



Tuy nhiên, vì Mobile App chắc chắn sẽ có, API cần được thiết kế để có thể hỗ trợ JWT hoặc token-based authentication.



Không tự ý thay đổi cơ chế đăng nhập hiện tại nếu chưa được yêu cầu.



Khi tạo API mới, phải xác định rõ:



\* API dùng cho Web, Mobile App hay cả hai.

\* API dùng Cookie Authentication hay JWT trong tương lai.

\* Role nào được phép gọi API.

\* Dữ liệu trả về có bị lộ thông tin nhạy cảm không.



Không được trả về:



\* PasswordHash

\* SecurityStamp

\* Token nội bộ

\* Thông tin nhạy cảm không cần thiết

\* Dữ liệu của trung tâm khác



\---



\## API Security Rules



Mọi API thay đổi dữ liệu phải có:



\* Authentication

\* Authorization theo role

\* Server-side validation

\* Anti-overposting protection

\* Error handling rõ ràng

\* Logging cần thiết

\* Kiểm tra quyền truy cập theo CenterId nếu dữ liệu thuộc trung tâm



Không cho phép API public thay đổi dữ liệu nếu chưa có thiết kế bảo mật rõ ràng.



\---



\## Mobile App Compatibility Rules



API phải được thiết kế để Mobile App gọi được ổn định.



Danh sách dữ liệu lớn phải hỗ trợ:



\* Pagination

\* Search

\* Filter

\* Sort



Query parameters tiêu chuẩn:



```txt

page

pageSize

keyword

sortBy

sortDirection

status

```



Response danh sách nên có metadata:



```json

{

&#x20; "success": true,

&#x20; "data": \[],

&#x20; "pagination": {

&#x20;   "page": 1,

&#x20;   "pageSize": 10,

&#x20;   "totalItems": 100,

&#x20;   "totalPages": 10

&#x20; }

}

```



Dữ liệu thời gian nên dùng chuẩn ISO 8601.



API không được phụ thuộc vào HTML, Razor Page hoặc ViewState.



\---



\## API Documentation Rules



Khi tạo API mới, cần cập nhật tài liệu trong:



```txt

/docs/api/

```



Mỗi API nên có:



\* Endpoint

\* Method

\* Request body

\* Response body

\* Role permission

\* Validation rules

\* Error cases



Nếu dự án có Swagger/OpenAPI, cần đảm bảo API hiển thị đúng trên Swagger.



\---



\## Development Workflow Update



Khi nhận yêu cầu phát triển chức năng mới, phải phân tích theo thứ tự:



1\. Business rules

2\. Database impact

3\. Service methods

4\. API endpoints

5\. Razor Pages UI

6\. Mobile App compatibility

7\. Security and authorization

8\. Validation rules

9\. Testing checklist



Không được thiết kế UI trước rồi mới nghĩ API sau.



\---



\## Important API Decision



EduBridge phải có khả năng vận hành qua API.



Nếu sau này bỏ Razor Pages và thay bằng Mobile App hoặc Frontend App riêng, hệ thống vẫn phải dùng lại được phần lớn business logic.



Khi có xung đột giữa:



\* Làm nhanh trực tiếp trong Razor Pages

\* Làm đúng kiến trúc API-first



Luôn ưu tiên kiến trúc API-first.


---

## API Documentation Rules

EduBridge là hệ thống API-first.

Mọi API mới được tạo hoặc chỉnh sửa đều phải cập nhật tài liệu API tương ứng.

### Mandatory Requirement

Khi tạo mới:

* API Controller
* API Endpoint
* Request DTO
* Response DTO

bắt buộc phải cập nhật tài liệu trong:

```txt
/docs/api/
```

Không được tạo API mà không có tài liệu.

---

## API Documentation Structure

Mỗi module có một file riêng:

```txt
/docs/api/classes.md
/docs/api/students.md
/docs/api/teachers.md
/docs/api/subjects.md
/docs/api/attendance.md
/docs/api/chat.md
```

Không gộp toàn bộ API vào một file quá lớn.

---

## Documentation Update Rule

Khi:

* Thêm endpoint mới
* Sửa request DTO
* Sửa response DTO
* Thay đổi business rule
* Thay đổi permission

=> Bắt buộc cập nhật file API document tương ứng.

Nếu tài liệu chưa được cập nhật thì xem như công việc chưa hoàn thành.

---

## Required API Documentation Content

Mỗi endpoint phải mô tả:

### Endpoint

```txt
POST /api/v1/classes
```

### Purpose

Mục đích nghiệp vụ.

### Roles

Các role được phép gọi.

Ví dụ:

```txt
OWNER
```

### Request

Ví dụ JSON request.

### Response

Ví dụ JSON response.

### Validation Rules

Toàn bộ rule validate.

### Error Cases

Các trường hợp lỗi.

Ví dụ:

```txt
Teacher schedule conflict
Room schedule conflict
Invalid start date
Unauthorized
```

### Notes

Lưu ý đặc biệt về nghiệp vụ.

---

## API Development Workflow

Khi phát triển API mới phải theo trình tự:

1. Business Analysis
2. Database Analysis
3. Service Design
4. API Design
5. API Documentation
6. API Implementation
7. UI Integration
8. Testing

Không được code API trước rồi mới viết tài liệu.

---

## Swagger Requirement

Nếu dự án sử dụng Swagger/OpenAPI:

* API phải hiển thị đúng trên Swagger.
* Summary và Description phải rõ ràng.
* Request DTO và Response DTO phải được mô tả đầy đủ.

---

## Completion Criteria

Một API chỉ được xem là hoàn thành khi:

* API hoạt động đúng.
* Có validation.
* Có authorization.
* Có Swagger description.
* Có tài liệu trong `/docs/api`.
* Có ví dụ request/response.
* Có mô tả error cases.

Nếu thiếu tài liệu API thì API được xem là chưa hoàn thành.

---

## Confirmation Action Rules

Các thao tác có khả năng làm thay đổi dữ liệu hoặc ảnh hưởng đến người dùng bắt buộc phải xác nhận trước khi thực hiện.

Ví dụ:

* Xóa dữ liệu
* Hủy lớp học
* Đóng lớp học
* Khóa tài khoản
* Vô hiệu hóa tài khoản
* Gỡ liên kết học sinh
* Chuyển trạng thái quan trọng
* Reset mật khẩu
* Xóa lịch học
* Xóa buổi học
* Xóa phòng học
* Xóa ca học
* Xóa giáo viên
* Xóa phụ huynh
* Xóa học sinh

---

## Modal Confirmation Requirement

Không sử dụng:

```javascript
confirm()
alert()
prompt()
```

Không sử dụng JavaScript browser dialog mặc định.

Bắt buộc sử dụng:

* Bootstrap Modal
* Hoặc component xác nhận chuẩn của hệ thống

để đảm bảo:

* Đồng bộ UX/UI
* Đồng bộ giao diện toàn hệ thống
* Responsive
* Hỗ trợ tùy biến nội dung

---


## UX Requirement

Modal phải hiển thị:

* Tên đối tượng liên quan
* Mã đối tượng nếu có
* Trạng thái hiện tại nếu cần

Ví dụ:

```txt
Lớp học:
IELTS Foundation K06

Bạn có chắc chắn muốn hủy lớp học này?
```

---

## Soft Delete Preference

Ưu tiên Soft Delete hơn Hard Delete.

Đối với dữ liệu nghiệp vụ quan trọng:

* Users
* Parents
* Students
* Teachers
* Classes
* Courses

không được Hard Delete nếu chưa được yêu cầu rõ ràng.

Ưu tiên:

```txt
Status = INACTIVE
Status = CANCELLED
IsDeleted = true
```

---

## Dangerous Action Rule

Đối với các hành động nguy hiểm:

* Xóa vĩnh viễn
* Reset mật khẩu
* Xóa dữ liệu có quan hệ

Modal phải hiển thị cảnh báo rõ ràng.

Nếu cần, yêu cầu nhập lại thông tin xác nhận.

Ví dụ:

```txt
Nhập DELETE để xác nhận.
```

---

## Implementation Rule

Khi phát triển chức năng mới:

Không được dùng:

```javascript
onclick="return confirm('...')"
```

Không được dùng:

```javascript
window.confirm(...)
```

Luôn sử dụng modal xác nhận chuẩn của hệ thống.

Mọi màn hình phải dùng cùng một mẫu modal để đảm bảo UX/UI nhất quán trên toàn bộ EduBridge.


\## Important



Dự án sẽ được triển khai production.



Mọi giải pháp cần:



\* Hiệu năng tốt

\* Bảo mật tốt

\* Dễ bảo trì

\* Tuân thủ thực tế triển khai doanh nghiệp

