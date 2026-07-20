EduBridge AI Agent Context
1. Project Overview
EduBridge là hệ thống quản lý trung tâm giáo dục.
Technology Stack
ASP.NET Core Razor Pages (.NET 8)
C#
Entity Framework Core
SQL Server
Database First
Cookie Authentication
SignalR (Realtime Chat)
Bootstrap 5
Visual Studio 2022
Language
Luôn trả lời bằng tiếng Việt.
Production Requirement
Dự án sẽ được triển khai production.
Mọi giải pháp cần:
Hiệu năng tốt
Bảo mật tốt
Dễ bảo trì
Tuân thủ thực tế triển khai doanh nghiệp
---
2. Core Development Principles
General Coding Rules
Ưu tiên code sạch, dễ bảo trì.
Không tạo code thừa.
Không refactor ngoài phạm vi yêu cầu.
Không tự ý đổi tên file, class, property hoặc table.
Không tự ý tạo migration nếu chưa được yêu cầu.
Không tự ý thay đổi cấu trúc database.
Không xoá dữ liệu hoặc thực hiện UPDATE/DELETE trên database khi chưa được yêu cầu rõ ràng.
Không sửa code khi chưa đọc đủ luồng liên quan.
Không suy đoán cấu trúc bảng, entity, property hoặc relationship.
Response Format When Supporting Code
Khi hỗ trợ code, luôn trình bày theo cấu trúc:
File cần sửa
Ví dụ:
```txt
Pages/Classes/Create.cshtml
```
Lý do sửa
Giải thích ngắn gọn vì sao cần sửa.
Code cần thay
Hiển thị đầy đủ code cần thay hoặc đầy đủ block liên quan.
Tác động
Liệt kê file, API, UI, service, database hoặc chức năng bị ảnh hưởng.
---
3. Strict Database First Rules
EduBridge sử dụng Entity Framework Core theo hướng Database First.
Database là nguồn dữ liệu gốc (Source of Truth).
Thứ tự bắt buộc:
```txt
Database
↓
DbContext
↓
Entity Models
↓
Repository / Service
↓
API Controller
↓
Razor Pages / Mobile App
```
Database Information
Tên database:
```txt
EduBridgeDB
```
ORM:
```txt
Entity Framework Core Database First
```
Database First Mandatory Rules
Khi cần thay đổi model:
Kiểm tra database trước.
Kiểm tra DbContext.
Kiểm tra Entity Model.
Kiểm tra Razor Page liên quan.
Kiểm tra Service / Repository liên quan.
Kiểm tra API Controller liên quan nếu có.
Kiểm tra tài liệu API nếu thay đổi ảnh hưởng API.
Không được:
Thiết kế Entity trước Database.
Thiết kế Model trước Database.
Sử dụng Code First nếu chưa được yêu cầu rõ ràng.
Tự ý sử dụng Add-Migration.
Tự ý sử dụng Update-Database.
Tự ý tạo Migration.
Tự ý sửa Entity Model thủ công nếu Entity được sinh từ Database First.
Suy đoán cấu trúc bảng.
Suy đoán kiểu dữ liệu.
Suy đoán quan hệ khóa ngoại.
Entity Regeneration Rules
Khi Database thay đổi:
Cập nhật SQL Script.
Cập nhật Database.
Regenerate DbContext và Entity Models.
Kiểm tra code bị ảnh hưởng.
Cập nhật Repository / Service.
Cập nhật API.
Cập nhật Razor Pages.
Cập nhật tài liệu API nếu có.
Build solution và kiểm tra lỗi.
Không được sửa Entity Model thủ công để thay thế cho việc cập nhật database.
---
4. Database Script Rules
Khi đề xuất thay đổi database:
Không chỉ mô tả bằng lời.
Bắt buộc cung cấp script SQL hoàn chỉnh có thể thực thi.
Ưu tiên thứ tự:
Script CREATE mới hoàn chỉnh nếu là bảng mới.
Script ALTER hoàn chỉnh nếu là chỉnh sửa bảng hiện có.
Script rollback nếu thay đổi có rủi ro cao.
Script seed data nếu có dữ liệu mẫu cần tạo.
Không chỉ hiển thị phần thay đổi nhỏ lẻ.
Ví dụ không được:
```sql
ALTER TABLE Classes
ADD Status NVARCHAR(50)
```
Phải hiển thị đầy đủ script liên quan để DBA có thể review.
Database Analysis Requirement
Trước khi đề xuất thay đổi database phải kiểm tra:
Existing tables
Existing columns
Foreign keys
Indexes
Constraints
Default values
Existing data impact
Không được suy đoán schema.
Full SQL Output Requirement
Khi người dùng yêu cầu:
Tạo bảng
Sửa bảng
Thêm cột
Thêm FK
Thêm Index
Thêm Constraint
Seed dữ liệu
Sửa lỗi liên quan database
Phải hiển thị:
Impact Analysis
Ảnh hưởng tới hệ thống.
SQL Script
Script hoàn chỉnh.
Rollback Script
Script rollback nếu khả thi.
Database Safety Rules
Không được đề xuất:
DROP TABLE
TRUNCATE TABLE
DELETE dữ liệu hàng loạt
nếu chưa được người dùng xác nhận rõ ràng.
Đối với production database:
Ưu tiên:
Soft delete
Backup trước khi migration
Script rollback
Kiểm tra dữ liệu hiện có trước khi ALTER
Migration Rules
Dự án mặc định không sử dụng EF Code First Migration.
Không đề xuất:
Add-Migration
Update-Database
Remove-Migration
trừ khi người dùng yêu cầu rõ ràng.
Nếu người dùng yêu cầu sử dụng Entity Framework Migration, bắt buộc hiển thị:
Migration Name
Migration Command
Generated SQL Script
Risk / Impact
Rollback plan nếu khả thi
Không chỉ hiển thị tên migration.
---
5. Mandatory Error Check
Trước khi đề xuất hoặc sửa code phải kiểm tra lỗi trong phạm vi liên quan.
Nếu bắt đầu phân tích toàn dự án, phải:
Đọc solution.
Đọc appsettings.json.
Đọc DbContext.
Đọc Entity Models.
Đọc Authentication configuration.
Đọc Razor Pages liên quan.
Đọc Service / Repository liên quan.
Đọc API Controller liên quan nếu có.
Đọc database schema qua MCP SQL Server nếu khả dụng.
Đối chiếu Code ↔ Database.
Build solution nếu môi trường cho phép.
Báo cáo các vấn đề phát hiện được.
Chỉ sửa code khi được yêu cầu.
Error Categories To Check
Ưu tiên kiểm tra:
Build error
Compile error
Runtime error
Warning quan trọng
Null reference risk
Dependency Injection error
Routing error
Razor Page ↔ PageModel mismatch
API ↔ Service mismatch
Entity ↔ Table mismatch
DbContext ↔ Database mismatch
Authentication / Authorization issue
Validation issue
Security issue
Performance issue
Dead code liên quan trực tiếp
UI/UX bug liên quan luồng đang sửa
---
6. MCP SQL Server Rules
Nếu MCP SQL Server khả dụng, ưu tiên đọc:
Tables
Columns
Foreign Keys
Indexes
Constraints
Default values
Sample Data nếu cần
Trước khi đưa ra bất kỳ đề xuất nào liên quan đến database.
Không được đưa ra kết luận về database nếu chưa kiểm tra schema hoặc chưa nói rõ giả định.
---
7. Project Architecture
Authentication
Hệ thống sử dụng Cookie Authentication.
Đăng nhập bằng:
Email
Hoặc số điện thoại
Không tự ý thay đổi cơ chế đăng nhập hiện tại nếu chưa được yêu cầu.
Roles
Chỉ có 3 vai trò chính:
OWNER
Chủ trung tâm.
TEACHER
Giáo viên.
PARENT
Phụ huynh.
---
8. API-First Architecture Rules
EduBridge chắc chắn sẽ có Mobile App và có thể có thêm Frontend App hoặc External Integration trong tương lai.
Vì vậy, mọi chức năng mới phải được thiết kế theo hướng API-first.
Mandatory API Requirement
Không được chỉ code Razor Pages.
Mỗi chức năng nghiệp vụ quan trọng phải có khả năng được gọi thông qua API để Mobile App có thể sử dụng.
Các module bắt buộc phải hỗ trợ API:
Subject Management
Teacher Management
Parent Management
Student Management
Class Management
Room Management
Shift Management
Attendance
Notification
Chat
Required Architecture
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
Hiển thị UI
Binding form
Validate cơ bản phía UI
Gọi Service
Điều hướng trang
Business logic bắt buộc phải đặt trong Service Layer để API và Razor Pages có thể dùng chung.
Business Logic Reuse Rule
Nếu một nghiệp vụ có thể được dùng bởi cả Web App và Mobile App, bắt buộc đưa vào Service.
Ví dụ:
Tạo lớp học
Cập nhật lớp học
Check trùng lịch giáo viên
Check trùng phòng học
Sinh buổi học
Gán học sinh vào lớp
Điểm danh
Gửi thông báo
Gửi tin nhắn
Xác thực quyền truy cập
Không được duplicate logic giữa:
Razor PageModel
API Controller
SignalR Hub
Important API Decision
EduBridge phải có khả năng vận hành qua API.
Nếu sau này bỏ Razor Pages và thay bằng Mobile App hoặc Frontend App riêng, hệ thống vẫn phải dùng lại được phần lớn business logic.
Khi có xung đột giữa:
Làm nhanh trực tiếp trong Razor Pages
Làm đúng kiến trúc API-first
Luôn ưu tiên kiến trúc API-first.
---
9. API Controller Rules
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
---
10. DTO and API Response Rules
API không nên trả trực tiếp Entity nếu không cần thiết.
Bắt buộc ưu tiên dùng:
Request DTO
Response DTO
ViewModel nếu dùng cho Razor Pages
ApiResponse wrapper nếu cần response thống nhất
Response thành công nên theo dạng:
```json
{
  "success": true,
  "message": "Success",
  "data": {}
}
```
Response lỗi nên theo dạng:
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": []
}
```
Không được trả về:
PasswordHash
SecurityStamp
Token nội bộ
Thông tin nhạy cảm không cần thiết
Dữ liệu của trung tâm khác
---
11. API Authentication and Authorization
Web hiện tại dùng Cookie Authentication.
Tuy nhiên, vì Mobile App chắc chắn sẽ có, API cần được thiết kế để có thể hỗ trợ JWT hoặc token-based authentication.
Không tự ý thay đổi cơ chế đăng nhập hiện tại nếu chưa được yêu cầu.
Khi tạo API mới, phải xác định rõ:
API dùng cho Web, Mobile App hay cả hai.
API dùng Cookie Authentication hay JWT trong tương lai.
Role nào được phép gọi API.
Dữ liệu trả về có bị lộ thông tin nhạy cảm không.
Dữ liệu có cần kiểm tra CenterId không.
API Security Rules
Mọi API thay đổi dữ liệu phải có:
Authentication
Authorization theo role
Server-side validation
Anti-overposting protection
Error handling rõ ràng
Logging cần thiết
Kiểm tra quyền truy cập theo CenterId nếu dữ liệu thuộc trung tâm
Không cho phép API public thay đổi dữ liệu nếu chưa có thiết kế bảo mật rõ ràng.
---
12. Mobile App Compatibility Rules
API phải được thiết kế để Mobile App gọi được ổn định.
Danh sách dữ liệu lớn phải hỗ trợ:
Pagination
Search
Filter
Sort
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
  "success": true,
  "data": [],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 100,
    "totalPages": 10
  }
}
```
Dữ liệu thời gian nên dùng chuẩn ISO 8601.
API không được phụ thuộc vào HTML, Razor Page hoặc ViewState.
---
13. API Documentation Rules
EduBridge là hệ thống API-first.
Mọi API mới được tạo hoặc chỉnh sửa đều phải cập nhật tài liệu API tương ứng.
Mandatory Requirement
Khi tạo mới hoặc chỉnh sửa:
API Controller
API Endpoint
Request DTO
Response DTO
Business rule
Permission
Validation rule
bắt buộc phải cập nhật tài liệu trong:
```txt
/docs/api/
```
Không được tạo API mà không có tài liệu.
API Documentation Structure
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
Required API Documentation Content
Mỗi endpoint phải mô tả:
Endpoint
```txt
POST /api/v1/classes
```
Purpose
Mục đích nghiệp vụ.
Roles
Các role được phép gọi.
Ví dụ:
```txt
OWNER
```
Request
Ví dụ JSON request.
Response
Ví dụ JSON response.
Validation Rules
Toàn bộ rule validate.
Error Cases
Các trường hợp lỗi.
Ví dụ:
```txt
Teacher schedule conflict
Room schedule conflict
Invalid start date
Unauthorized
```
Notes
Lưu ý đặc biệt về nghiệp vụ.
Swagger Requirement
Nếu dự án sử dụng Swagger/OpenAPI:
API phải hiển thị đúng trên Swagger.
Summary và Description phải rõ ràng.
Request DTO và Response DTO phải được mô tả đầy đủ.
API Completion Criteria
Một API chỉ được xem là hoàn thành khi:
API hoạt động đúng.
Có validation.
Có authorization.
Có Swagger description.
Có tài liệu trong `/docs/api`.
Có ví dụ request/response.
Có mô tả error cases.
Nếu thiếu tài liệu API thì API được xem là chưa hoàn thành.
---
14. Development Workflow
Khi nhận yêu cầu mới, phải phân tích theo thứ tự:
Business rules
Database impact
Service methods
API endpoints
API documentation
Razor Pages UI
Mobile App compatibility
Security and authorization
Validation rules
Testing checklist
Không được thiết kế UI trước rồi mới nghĩ API sau.
Implementation Workflow
Khi nhận yêu cầu mới:
Bước 1
Phân tích chức năng.
Bước 2
Xác định các file liên quan:
Razor Page (.cshtml)
PageModel (.cshtml.cs)
DbContext
Entity Model
Service
Repository
API Controller
DTO / ViewModel
API documentation
SQL script nếu có thay đổi database
Bước 3
Đọc toàn bộ luồng hiện tại.
Bước 4
Đề xuất phương án.
Bước 5
Mới tiến hành sửa code.
---
15. UI/UX Rules
Ưu tiên giao diện quản trị chuyên nghiệp.
Responsive.
Sử dụng Bootstrap 5.
Tránh popup lồng popup.
Ưu tiên modal cho CRUD đơn giản.
Ưu tiên page riêng cho quy trình nhiều bước.
Form validation phải có cả Client-side và Server-side.
UI phải đồng bộ với design system hiện có nếu dự án đã có.
Không làm UI tách rời business rule.
---
16. Confirmation Action Rules
Các thao tác có khả năng làm thay đổi dữ liệu hoặc ảnh hưởng đến người dùng bắt buộc phải xác nhận trước khi thực hiện.
Ví dụ:
Xóa dữ liệu
Hủy lớp học
Đóng lớp học
Khóa tài khoản
Vô hiệu hóa tài khoản
Gỡ liên kết học sinh
Chuyển trạng thái quan trọng
Reset mật khẩu
Xóa lịch học
Xóa buổi học
Xóa phòng học
Xóa ca học
Xóa giáo viên
Xóa phụ huynh
Xóa học sinh
Modal Confirmation Requirement
Không sử dụng:
```javascript
confirm()
alert()
prompt()
```
Không sử dụng JavaScript browser dialog mặc định.
Bắt buộc sử dụng:
Bootstrap Modal
Hoặc component xác nhận chuẩn của hệ thống
để đảm bảo:
Đồng bộ UX/UI
Đồng bộ giao diện toàn hệ thống
Responsive
Hỗ trợ tùy biến nội dung
Confirmation Modal UX Requirement
Modal phải hiển thị:
Tên đối tượng liên quan
Mã đối tượng nếu có
Trạng thái hiện tại nếu cần
Hành động sắp thực hiện
Cảnh báo nếu hành động có rủi ro
Ví dụ:
```txt
Lớp học:
IELTS Foundation K06

Bạn có chắc chắn muốn hủy lớp học này?
```
Dangerous Action Rule
Đối với các hành động nguy hiểm:
Xóa vĩnh viễn
Reset mật khẩu
Xóa dữ liệu có quan hệ
Thay đổi trạng thái ảnh hưởng nhiều người dùng
Modal phải hiển thị cảnh báo rõ ràng.
Nếu cần, yêu cầu nhập lại thông tin xác nhận.
Ví dụ:
```txt
Nhập DELETE để xác nhận.
```
Implementation Rule
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
---
17. Soft Delete Rules
Ưu tiên Soft Delete hơn Hard Delete.
Đối với dữ liệu nghiệp vụ quan trọng:
Users
Parents
Students
Teachers
Classes
Courses
Rooms
Shifts
không được Hard Delete nếu chưa được yêu cầu rõ ràng.
Ưu tiên:
```txt
Status = INACTIVE
Status = CANCELLED
IsDeleted = true
```
Nếu database hiện tại chưa có cột hỗ trợ soft delete, phải phân tích schema trước và đề xuất SQL script hoàn chỉnh nếu cần bổ sung.
---
18. Module Business Rules
Class Management Rules
Class Status:
UPCOMING
ACTIVE
COMPLETED
CANCELLED
Teacher Management Rules
Teacher Status:
ACTIVE
INACTIVE
Parent Management Rules
Mỗi phụ huynh có thể quản lý nhiều học sinh.
Mỗi học sinh chỉ thuộc một phụ huynh chính.
Student Management Rules
StudentCode được hệ thống tự sinh.
Không yêu cầu email hoặc số điện thoại đối với học sinh nhỏ tuổi.
Chat Rules
SignalR Realtime Chat.
Không polling.
Tin nhắn phải được lưu database.
Không duplicate logic giữa SignalR Hub và Service.
Business logic của chat phải đưa vào Service nếu API hoặc Mobile App cần dùng lại.
Attendance Rules
Điểm danh theo từng buổi học.
Mỗi bản ghi điểm danh phải gắn với:
Student
Lesson
Attendance Status
---
19. Deployment Readiness Check
Trước khi merge hoặc release, cần kiểm tra:
Build thành công 100%.
Không còn compile error.
Không còn warning nghiêm trọng.
Không còn migration pending hoặc schema mismatch.
Connection string được cấu hình đúng.
Không commit secret thật trong appsettings.json.
API Swagger chạy được nếu dự án có Swagger.
Authentication hoạt động.
Authorization theo role hoạt động.
SignalR hoạt động nếu có thay đổi Chat.
Các màn hình CRUD chính hoạt động.
API documentation đã cập nhật.
Không có dữ liệu nhạy cảm hard-code.
Không có endpoint public thay đổi dữ liệu khi chưa có bảo mật.
---
20. Final Completion Checklist
Một task chỉ được xem là hoàn thành khi:
Đã đọc đúng file liên quan.
Đã kiểm tra Database First impact.
Đã kiểm tra Service / API / UI impact.
Đã có full SQL script nếu có thay đổi database.
Đã có rollback script nếu thay đổi database có rủi ro.
Đã cập nhật tài liệu API nếu có API mới hoặc thay đổi API.
Đã kiểm tra authentication / authorization nếu chức năng liên quan quyền.
Đã kiểm tra validation client-side và server-side nếu có form.
Đã kiểm tra responsive nếu có UI.
Đã báo cáo rõ file đã sửa và tác động.
Đã build hoặc nêu rõ chưa thể build nếu môi trường không cho phép.