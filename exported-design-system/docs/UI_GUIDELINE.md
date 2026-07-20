# EduBridge Exported UI Design System

Bộ export này được tạo để copy sang project `MMO_System` dùng Spring Boot/IntelliJ. CSS độc lập, không phụ thuộc ASP.NET Core Razor, Tailwind runtime, hay Tag Helper.

## Component đã export

- Design tokens: màu, font, border, radius, shadow.
- Buttons: primary, secondary/reset filter, danger, outline, success.
- Icons/action buttons: view/edit, delete, reset/refresh.
- Forms: input, select, textarea, date field, radio, validation text.
- Filter/search/dropdown/date range.
- Tables: header, row hover, avatar/entity cell, badge, action cell.
- Pagination.
- Toggle/switch trạng thái.
- Toast notification stacked.
- Datepicker độc lập cho form giáo viên/học sinh/phụ huynh.

## Copy sang Spring Boot

Copy thư mục `exported-design-system/css` sang:

```text
src/main/resources/static/css/
```

Nếu dùng Thymeleaf, import CSS:

```html
<link rel="stylesheet" th:href="@{/css/design-system.css}">
<link rel="stylesheet" th:href="@{/css/buttons.css}">
<link rel="stylesheet" th:href="@{/css/forms.css}">
<link rel="stylesheet" th:href="@{/css/filters.css}">
<link rel="stylesheet" th:href="@{/css/tables.css}">
<link rel="stylesheet" th:href="@{/css/pagination.css}">
<link rel="stylesheet" th:href="@{/css/toggle.css}">
<link rel="stylesheet" th:href="@{/css/toast.css}">
<link rel="stylesheet" th:href="@{/css/datepicker.css}">
<script th:src="@{/js/datepicker.js}"></script>
```

Nếu dùng JSP:

```html
<link rel="stylesheet" href="${pageContext.request.contextPath}/css/design-system.css">
<link rel="stylesheet" href="${pageContext.request.contextPath}/css/buttons.css">
<script src="${pageContext.request.contextPath}/js/datepicker.js"></script>
```

## Datepicker cho màn hình quản lý giáo viên

HTML dùng trong Thymeleaf:

```html
<div class="ds-datepicker" data-ds-datepicker>
    <input type="hidden" th:field="*{dateOfBirth}" data-ds-date-value>
    <div class="ds-datepicker-field">
        <input class="ds-datepicker-input" type="text" placeholder="dd/mm/yyyy" data-ds-date-display>
        <button type="button" class="ds-datepicker-button" data-ds-date-toggle aria-label="Chọn ngày">
            <svg width="18" height="18" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3M4 11h16M5 5h14a1 1 0 011 1v14a1 1 0 01-1 1H5a1 1 0 01-1-1V6a1 1 0 011-1z"></path>
            </svg>
        </button>
    </div>
</div>
```

Input người dùng thấy là `dd/mm/yyyy`. Hidden input gửi về server dạng `yyyy-MM-dd`, phù hợp `LocalDate` của Spring Boot.

## Quy định sử dụng

- Dùng class prefix `.ds-` cho UI trong MMO.
- Không dùng inline style nếu đã có class export tương ứng.
- Logic như phân trang, filter, dropdown search, toast stacking cần viết lại bằng JavaScript/Thymeleaf/Spring Controller của MMO. Datepicker single-date đã có file JS độc lập trong `js/datepicker.js`.
- Các file HTML trong `examples/` mở trực tiếp bằng browser để xem nhanh hình dạng component.
