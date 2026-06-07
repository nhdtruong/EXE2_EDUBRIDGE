# Export Report

## File đã quét

- `wwwroot/css/site.css`
- `wwwroot/css/theme.css`
- `wwwroot/css/teacher.css`
- `wwwroot/js/site.js`
- `Pages/Shared/_AdminLayout.cshtml`
- `Pages/Shared/_TeacherLayout.cshtml`
- `Pages/Login.cshtml`
- `Pages/AdminDashboard.cshtml`
- `Pages/AdminClasses.cshtml`
- `Pages/AdminCourses.cshtml`
- `Pages/AdminTeachers.cshtml`
- `Pages/AdminTeachers/Create.cshtml`
- `Pages/AdminTeachers/Edit.cshtml`
- `Pages/AdminStudents.cshtml`
- `Pages/AdminStudents/Create.cshtml`
- `Pages/AdminStudents/Edit.cshtml`
- `Pages/AdminStudents/ParentEdit.cshtml`
- `Pages/AdminFinance.cshtml`
- `Pages/AdminSettings.cshtml`
- `Pages/Attendance.cshtml`
- `Pages/Grades.cshtml`
- `Pages/Homework.cshtml`
- `Pages/Messages.cshtml`
- `Pages/Teacher/*.cshtml`

Không có thư mục `Views/` trong project hiện tại.

## Component tìm thấy và đã export

| Component | File export | Class gốc EduBridge | Class export |
| --- | --- | --- | --- |
| Design tokens | `css/design-system.css` | `:root` trong `site.css`, `theme.css`; Tailwind utility admin | `:root --ds-*` |
| Card/panel | `css/design-system.css` | `rounded-2xl border bg-white shadow-sm`, `.dashboard-card`, `.stat-card` | `.ds-card`, `.ds-card-header` |
| Primary button | `css/buttons.css` | `.btn-primary`, `bg-blue-600 hover:bg-blue-700` | `.ds-btn .ds-btn-primary` |
| Secondary/reset button | `css/buttons.css` | `bg-slate-600 hover:bg-slate-700` | `.ds-btn .ds-btn-secondary` |
| Danger button | `css/buttons.css` | `bg-red-600 hover:bg-red-700` | `.ds-btn .ds-btn-danger` |
| Outline/cancel button | `css/buttons.css` | `border border-gray-300 bg-white` | `.ds-btn .ds-btn-outline` |
| Success button | `css/buttons.css` | `.btn-primary-green`, `.btn-save` | `.ds-btn .ds-btn-success` |
| Form controls | `css/forms.css` | `.form-control`, admin `input/select/textarea` utilities | `.ds-input`, `.ds-select`, `.ds-textarea` |
| Date field | `css/forms.css` | `[data-date-toggle]` calendar button | `.ds-date-field`, `.ds-date-button` |
| Filter/search/dropdown | `css/filters.css` | `[data-search-dropdown]`, `[data-dropdown-toggle]` | `.ds-filter-*`, `.ds-dropdown-*` |
| Date range | `css/filters.css` | `[data-date-range]`, `[data-range-toggle]` | `.ds-date-range-*` |
| Table | `css/tables.css` | admin tables using `w-full border-collapse`, `.attendance-table`, `.grades-table` | `.ds-table-*` |
| Pagination | `css/pagination.css` | Admin pagination block | `.ds-pagination-*` |
| Toggle | `css/toggle.css` | status toggle button with track/knob | `.ds-toggle-*` |
| Toast | `css/toast.css` | `layout-toast-container`, login/admin/finance/settings toast | `.ds-toast-*` |
| Action icon buttons | `css/icons.css` | view/delete/reset icon buttons | `.ds-icon-btn-*` |
| Datepicker | `css/datepicker.css`, `js/datepicker.js` | `wwwroot/js/site.js`, `[data-date-hidden]`, `[data-date-toggle]` | `.ds-datepicker-*`, `[data-ds-datepicker]` |

## Component không tìm thấy

- Sort UI reusable: chưa thấy sort arrow hoặc sort header component rõ ràng.
- Filter icon/funnel riêng: chưa thấy icon filter riêng, chỉ có filter bằng input/dropdown.
- CSS icon font riêng: chưa thấy Font Awesome/Bootstrap Icons được dùng làm nguồn chính.

## Ghi chú copy sang MMO

Có thể copy ngay:

- `design-system.css`
- `buttons.css`
- `forms.css`
- `filters.css`
- `tables.css`
- `pagination.css`
- `toggle.css`
- `toast.css`
- `icons.css`
- `datepicker.css`
- `js/datepicker.js`

Cần viết lại logic khi sang MMO:

- Submit filter/search bằng Spring Controller hoặc fetch API.
- Phân trang: backend phải nhận page/pageSize và trả total record.
- Dropdown search: cần JS riêng nếu muốn search option.
- Datepicker single-date: đã export CSS và JS độc lập, dùng được cho form giáo viên. Date range vẫn chỉ export hình dạng, nếu MMO cần range picker thì viết thêm logic.
- Toast: CSS đã export, JS show/hide/stacking cần viết lại theo MMO.

## Giá trị suy ra trực tiếp

- `--ds-primary` lấy từ admin button `bg-blue-600` tương đương `#2563eb`.
- `--ds-primary-hover` lấy từ `hover:bg-blue-700` tương đương `#1d4ed8`.
- `--ds-bg` lấy từ `_AdminLayout.cshtml` body background `#f8fafc`.
- `--ds-font-family` lấy từ Google font Inter trong layout và `site.css`.
- Radius/shadow lấy từ `rounded`, `rounded-xl`, `rounded-2xl`, `shadow-sm`, `shadow-xl` đang dùng trong Razor/Tailwind.
