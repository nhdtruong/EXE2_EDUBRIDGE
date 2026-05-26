function previewAvatar(input) {
    const file = input.files && input.files[0];
    const preview = document.getElementById('avatarPreview');
    const placeholder = document.getElementById('avatarPlaceholder');
    const removeInput = document.getElementById('Input_RemoveAvatar');

    if (!file) {
        return;
    }

    if (removeInput) {
        removeInput.value = 'false';
    }

    preview.src = URL.createObjectURL(file);
    preview.classList.remove('hidden');
    placeholder.classList.add('hidden');
}

function clearAvatar(markRemove) {
    const input = document.getElementById('avatarFileInput');
    const preview = document.getElementById('avatarPreview');
    const placeholder = document.getElementById('avatarPlaceholder');
    const removeInput = document.getElementById('Input_RemoveAvatar');

    input.value = '';
    preview.removeAttribute('src');
    preview.classList.add('hidden');
    placeholder.classList.remove('hidden');

    if (removeInput && markRemove) {
        removeInput.value = 'true';
    }
}

(function initTeacherDatePickers() {
    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const weekNames = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

    document.querySelectorAll('[data-date-hidden]').forEach(hidden => {
        const key = hidden.dataset.dateHidden;
        const display = document.querySelector(`[data-date-display="${key}"]`);
        const toggle = document.querySelector(`[data-date-toggle="${key}"]`);

        if (!display || !toggle) {
            return;
        }

        const initialDate = parseDateValue(hidden.value);
        const state = {
            mode: 'days',
            selected: initialDate,
            cursor: initialDate || new Date()
        };

        if (state.selected) {
            hidden.value = toIsoDate(state.selected);
            display.value = formatDisplayDate(state.selected);
        }

        const picker = document.createElement('div');
        picker.className = 'absolute left-0 top-[calc(100%+2px)] z-[70] hidden w-full min-w-[520px] border border-gray-300 bg-white text-sm text-gray-600 shadow-lg';
        picker.dataset.teacherDatePicker = 'true';
        display.parentElement.appendChild(picker);

        function render() {
            if (state.mode === 'months') {
                renderMonths();
                return;
            }

            if (state.mode === 'years') {
                renderYears();
                return;
            }

            renderDays();
        }

        function renderShell(title, bodyHtml) {
            picker.innerHTML = `
                <div class="flex h-12 items-center justify-between border-b border-gray-200 px-4">
                    <button type="button" data-nav="prev" class="flex h-8 w-8 items-center justify-center rounded hover:bg-gray-100">&lsaquo;</button>
                    <button type="button" data-switch-view class="rounded bg-slate-100 px-3 py-1 font-medium text-gray-700 hover:bg-slate-200">${title}</button>
                    <button type="button" data-nav="next" class="flex h-8 w-8 items-center justify-center rounded hover:bg-gray-100">&rsaquo;</button>
                </div>
                ${bodyHtml}
                <div class="flex h-12 items-center justify-between border-t border-gray-200 px-4">
                    <button type="button" data-today class="font-medium text-sky-700">Today</button>
                    <button type="button" data-clear class="font-medium text-slate-600">Clear</button>
                </div>
            `;

            picker.querySelector('[data-today]').addEventListener('click', () => selectDate(new Date()));
            picker.querySelector('[data-clear]').addEventListener('click', () => {
                state.selected = null;
                hidden.value = '';
                display.value = '';
                closePicker();
            });

            picker.querySelector('[data-switch-view]').addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                switchView();
            });

            picker.querySelectorAll('[data-nav]').forEach(button => {
                button.addEventListener('click', event => {
                    event.preventDefault();
                    event.stopPropagation();
                    moveCursor(button.dataset.nav === 'prev' ? -1 : 1);
                });
            });
        }

        picker.addEventListener('click', event => {
            event.stopPropagation();
            const switchButton = event.target.closest('[data-switch-view]');
            const navButton = event.target.closest('[data-nav]');
            const dateButton = event.target.closest('[data-date]');
            const monthButton = event.target.closest('[data-month]');
            const yearButton = event.target.closest('[data-year]');

            if (switchButton) {
                return;
            }

            if (navButton) {
                return;
            }

            if (dateButton) {
                selectDate(parseDateValue(dateButton.dataset.date));
                return;
            }

            if (monthButton) {
                state.cursor = new Date(state.cursor.getFullYear(), Number(monthButton.dataset.month), 1);
                state.mode = 'days';
                render();
                return;
            }

            if (yearButton) {
                state.cursor = new Date(Number(yearButton.dataset.year), state.cursor.getMonth(), 1);
                state.mode = 'months';
                render();
            }
        });

        picker.addEventListener('mousedown', event => {
            event.preventDefault();
            event.stopPropagation();
        });

        function renderDays() {
            const year = state.cursor.getFullYear();
            const month = state.cursor.getMonth();
            const firstDay = new Date(year, month, 1);
            const start = new Date(year, month, 1 - firstDay.getDay());
            const cells = [];

            for (let i = 0; i < 42; i += 1) {
                const date = new Date(start);
                date.setDate(start.getDate() + i);
                const isCurrentMonth = date.getMonth() === month;
                const isSelected = state.selected && sameDate(date, state.selected);
                cells.push(`
                    <button type="button" data-date="${toIsoDate(date)}" class="mx-auto flex h-9 w-9 items-center justify-center rounded-full ${isSelected ? 'bg-slate-300 text-gray-900' : 'hover:bg-slate-100'} ${isCurrentMonth ? 'text-gray-700' : 'text-gray-400'}">
                        ${date.getDate()}
                    </button>
                `);
            }

            renderShell(
                `${monthNames[month]} ${year}`,
                `
                    <div class="grid grid-cols-7 px-4 pt-4 text-center font-semibold text-gray-700">
                        ${weekNames.map(day => `<div>${day}</div>`).join('')}
                    </div>
                    <div class="grid grid-cols-7 gap-y-2 px-4 py-4 text-center">
                        ${cells.join('')}
                    </div>
                `);

        }

        function renderMonths() {
            const year = state.cursor.getFullYear();
            renderShell(
                year,
                `<div class="grid grid-cols-3 gap-y-4 px-12 py-5 text-center">
                    ${monthNames.map((month, index) => `<button type="button" data-month="${index}" class="rounded py-1 hover:bg-slate-100">${month}</button>`).join('')}
                </div>`);

        }

        function renderYears() {
            const year = state.cursor.getFullYear();
            const startYear = Math.floor(year / 10) * 10;
            const years = Array.from({ length: 10 }, (_, index) => startYear + index);
            renderShell(
                `${startYear} - ${startYear + 9}`,
                `<div class="grid grid-cols-2 gap-y-4 px-24 py-5 text-center">
                    ${years.map(item => `<button type="button" data-year="${item}" class="rounded py-1 hover:bg-slate-100">${item}</button>`).join('')}
                </div>`);

        }

        function selectDate(date) {
            state.selected = date;
            state.cursor = date;
            hidden.value = toIsoDate(date);
            display.value = formatDisplayDate(date);
            closePicker();
        }

        function switchView() {
            if (state.mode === 'days') {
                state.mode = 'months';
            } else if (state.mode === 'months') {
                state.mode = 'years';
            } else {
                state.mode = 'days';
            }

            render();
        }

        function moveCursor(direction) {
            if (state.mode === 'days') {
                state.cursor = new Date(state.cursor.getFullYear(), state.cursor.getMonth() + direction, 1);
            } else if (state.mode === 'months') {
                state.cursor = new Date(state.cursor.getFullYear() + direction, state.cursor.getMonth(), 1);
            } else {
                state.cursor = new Date(state.cursor.getFullYear() + direction * 10, state.cursor.getMonth(), 1);
            }

            render();
        }

        function openPicker() {
            document.querySelectorAll('[data-teacher-date-picker]').forEach(item => item.classList.add('hidden'));
            state.mode = 'days';
            render();
            picker.classList.remove('hidden');
        }

        function closePicker() {
            picker.classList.add('hidden');
        }

        display.addEventListener('input', () => {
            display.value = formatTypedDate(display.value);
        });
        display.addEventListener('blur', () => syncTypedDate());
        display.addEventListener('change', () => syncTypedDate());
        display.addEventListener('click', openPicker);
        toggle.addEventListener('click', openPicker);

        document.addEventListener('click', event => {
            if (!display.parentElement.contains(event.target)) {
                closePicker();
            }
        });

        function syncTypedDate() {
            const typedDate = parseDisplayDate(display.value);

            if (!typedDate) {
                if (!display.value.trim()) {
                    hidden.value = '';
                }

                return;
            }

            state.selected = typedDate;
            state.cursor = typedDate;
            hidden.value = toIsoDate(typedDate);
            display.value = formatDisplayDate(typedDate);
        }
    });

    function parseDateValue(value) {
        if (!value) {
            return null;
        }

        const normalizedValue = value.trim();
        const isoMatch = normalizedValue.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/);

        if (isoMatch) {
            return createValidDate(
                Number(isoMatch[1]),
                Number(isoMatch[2]),
                Number(isoMatch[3]));
        }

        const displayDate = parseDisplayDate(normalizedValue);

        if (displayDate) {
            return displayDate;
        }

        const digits = normalizedValue.replace(/\D/g, '');

        if (digits.length === 8) {
            return createValidDate(
                Number(digits.slice(4, 8)),
                Number(digits.slice(2, 4)),
                Number(digits.slice(0, 2)));
        }

        return null;
    }

    function createValidDate(year, month, day) {
        const date = new Date(year, month - 1, day);

        if (date.getFullYear() !== year ||
            date.getMonth() !== month - 1 ||
            date.getDate() !== day) {
            return null;
        }

        return date;
    }

    function toIsoDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');

        return `${year}-${month}-${day}`;
    }

    function formatDisplayDate(date) {
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');

        return `${day}/${month}/${date.getFullYear()}`;
    }

    function parseDisplayDate(value) {
        const match = value.trim().match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);

        if (!match) {
            return null;
        }

        const day = Number(match[1]);
        const month = Number(match[2]);
        const year = Number(match[3]);
        return createValidDate(year, month, day);
    }

    function formatTypedDate(value) {
        const digits = value.replace(/\D/g, '').slice(0, 8);

        if (digits.length <= 2) {
            return digits;
        }

        if (digits.length <= 4) {
            return `${digits.slice(0, 2)}/${digits.slice(2)}`;
        }

        return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
    }

    function sameDate(left, right) {
        return left.getFullYear() === right.getFullYear() &&
            left.getMonth() === right.getMonth() &&
            left.getDate() === right.getDate();
    }
})();
