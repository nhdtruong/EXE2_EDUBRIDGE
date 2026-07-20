# EduBridge Icon Guideline

Nguồn icon đang thấy trong EduBridge:

- Inline SVG trực tiếp trong Razor: phần lớn icon ở sidebar, button, action table, search, calendar, logout.
- Kiểu stroke/icon giống Lucide, nhưng project không import thư viện icon riêng cho admin UI.
- Không thấy CSS icon font riêng như Font Awesome hoặc Bootstrap Icons trong UI admin hiện tại.

Icon/action đã thấy:

| Action | Đã thấy | Nguồn/cách dùng |
| --- | --- | --- |
| Search | Có | Inline SVG kính lúp trong filter/search button |
| Trash can | Có | Inline SVG trong action xóa |
| Plus | Có | Inline SVG trong nút thêm mới |
| Angle Left | Có | Ký tự `&lsaquo;`, `&laquo;` ở pagination |
| Angle Right | Có | Ký tự `&rsaquo;`, `&raquo;` ở pagination |
| Edit / View | Có | Icon mắt inline SVG cho xem/sửa |
| Toggle | Có | CSS track/knob, không phải icon SVG |
| Pagination | Có | Ký tự điều hướng |
| Toast Notification | Có | Inline SVG/check/close trong layout toast |
| Calendar | Có | Inline SVG lịch ở datepicker |
| Refresh/reset | Có | Inline SVG cấp lại mật khẩu nhanh |
| Filter | Chưa thấy icon filter riêng | EduBridge dùng ô filter/dropdown, chưa thấy icon funnel |
| Sort arrows | Chưa thấy | Chưa thấy UI sort reusable |

Khuyến nghị khi mang sang MMO_System:

- Copy SVG inline từ ví dụ hoặc dùng cùng style stroke `fill="none" stroke="currentColor" stroke-width="2"`.
- Nếu MMO dùng thư viện icon, chọn Lucide tương đương để đồng bộ nét icon.
- Không tự thêm filter/sort icon nếu chưa có yêu cầu UI thật.

