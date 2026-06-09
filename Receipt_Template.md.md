# Receipt Template Specification

## Purpose

Biên lai thu tiền dùng để:

* In trực tiếp từ hệ thống EduBridge
* Xuất PDF
* Gửi phụ huynh
* Lưu lịch sử giao dịch

Mẫu biên lai tham khảo theo mẫu biên lai thu học phí truyền thống của trung tâm giáo dục.

---

# General Rules

* Hình thức biên lai phải gần giống mẫu chuẩn được cung cấp.
* Cho phép điều chỉnh khoảng cách và bố cục để phù hợp in PDF.
* Không được bỏ các thông tin nghiệp vụ quan trọng.
* Được phép thêm các trường mới nếu dữ liệu đã tồn tại trong DB.
* Mọi thay đổi phải dựa trên Database hiện có.

---

# Receipt Header

## Center Information

Hiển thị:

* Tên trung tâm
* Địa chỉ trung tâm
* Số điện thoại trung tâm (nếu có)
* Email trung tâm (nếu có)

Ví dụ:

Đơn vị: EduBridge Center

Địa chỉ: 123 Nguyễn Trãi, Hà Nội

---

# Receipt Information

Hiển thị:

* Receipt Number
* Invoice Number
* Receipt Date
* Payment Method

Ví dụ:

Số biên lai: RC202600001

Mã hóa đơn: INV202600001

Ngày thu: 01/08/2026

Hình thức thanh toán: Chuyển khoản

---

# Student Information

Bắt buộc hiển thị:

* Họ tên học sinh
* Mã học sinh
* Lớp học
* Khóa học

Nếu DB không có:

* Mã học sinh
* Khóa học

thì báo limitation và dùng dữ liệu hiện có.

---

# Parent Information

Nếu DB có dữ liệu:

Hiển thị:

* Họ tên phụ huynh
* Số điện thoại

Nếu DB không có thì bỏ qua.

---

# Payment Details

Nội dung thu:

Ví dụ:

* Học phí khóa học
* Phí giáo trình
* Phí thi
* Khoản thu khác

Hiển thị:

* Description
* Amount

Nếu hiện tại hệ thống chỉ hỗ trợ học phí:

Hiển thị:

Học phí lớp học

---

# Financial Summary

Hiển thị:

Tổng học phí

Giảm giá

Đã thanh toán

Còn nợ

Số tiền thu lần này

Tổng tiền thu

Số tiền bằng chữ

---

# Signature Area

Người nộp tiền

(Ký và ghi rõ họ tên)

Người thu tiền

(Ký và ghi rõ họ tên)

---

# Additional System Information

Nếu DB có dữ liệu:

Hiển thị:

* Người tạo biên lai
* Thời gian tạo
* Mã giao dịch chuyển khoản

---

# PDF Export Requirements

* Khổ giấy A4
* Hỗ trợ in đen trắng
* Font Unicode tiếng Việt
* Không bị vỡ layout khi in
* Có thể xuất PDF từ trình duyệt

---

# Database Compatibility Rule

Khi triển khai:

1. Phải đọc DB hiện tại trước.
2. Mapping từng trường với DB hiện có.
3. Không tự tạo dữ liệu giả.
4. Không bỏ các trường nghiệp vụ quan trọng.
5. Nếu DB thiếu trường:

   * Báo cáo limitation.
   * Đề xuất bổ sung.
   * Không tự ý sửa DB.

---

# Mandatory Fields

Không được bỏ:

* Receipt Number
* Invoice Number
* Receipt Date
* Student Name
* Class Name
* Amount Paid
* Total Amount
* Amount In Words
* Payment Method
* Signature Area

Các trường khác có thể điều chỉnh theo DB thực tế.
