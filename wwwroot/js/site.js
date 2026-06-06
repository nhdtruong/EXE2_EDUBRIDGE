// Shared UI helpers for EduBridge.
(function initEduBridgeDatePickers() {
    const monthNames = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
    ];
    const shortMonthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const weekNames = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

    document.addEventListener('DOMContentLoaded', () => {
        initSingleDatePickers();
        initRangeDatePickers();
    });

    function initSingleDatePickers() {
        document.querySelectorAll('[data-date-hidden]').forEach(hidden => {
            if (hidden.dataset.ebDateReady === 'true') {
                return;
            }

            const key = hidden.dataset.dateHidden;
            const display = document.querySelector(`[data-date-display="${key}"]`);
            const toggle = document.querySelector(`[data-date-toggle="${key}"]`);

            if (!display || !toggle) {
                return;
            }

            hidden.dataset.ebDateReady = 'true';
            const initialDate = parseDateValue(hidden.value || display.value);
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
            picker.className = 'absolute left-0 top-[calc(100%+2px)] z-[90] hidden w-[380px] max-w-[calc(100vw-2rem)] border border-gray-200 bg-white text-sm text-gray-600 shadow-xl';
            picker.dataset.ebDatePanel = 'true';
            display.parentElement.appendChild(picker);

            const selectDate = date => {
                state.selected = date;
                state.cursor = date;
                hidden.value = toIsoDate(date);
                display.value = formatDisplayDate(date);
                hidden.dispatchEvent(new Event('change', { bubbles: true }));
                picker.classList.add('hidden');
            };

            const openPicker = () => {
                closeAllDatePanels(picker);
                state.mode = 'days';
                renderSinglePicker(picker, state, selectDate);
                picker.classList.remove('hidden');
            };

            display.addEventListener('input', () => {
                display.value = formatTypedDate(display.value);
            });
            display.addEventListener('blur', () => syncTypedDate(display, hidden, state));
            display.addEventListener('change', () => syncTypedDate(display, hidden, state));
            display.addEventListener('click', openPicker);
            toggle.addEventListener('click', event => {
                event.stopPropagation();
                openPicker();
            });

            picker.addEventListener('mousedown', event => {
                event.preventDefault();
                event.stopPropagation();
            });
            picker.addEventListener('click', event => {
                event.stopPropagation();
            });
        });
    }

    function initRangeDatePickers() {
        document.querySelectorAll('[data-date-range]').forEach(range => {
            if (range.dataset.ebRangeReady === 'true') {
                return;
            }

            const toggle = range.querySelector('[data-range-toggle]');
            const label = range.querySelector('[data-range-label]');
            const hiddenFrom = range.querySelector('[data-range-from]');
            const hiddenTo = range.querySelector('[data-range-to]');

            if (!toggle || !label || !hiddenFrom || !hiddenTo) {
                return;
            }

            range.dataset.ebRangeReady = 'true';
            range.querySelector('[data-range-panel]')?.remove();

            const from = parseDateValue(hiddenFrom.value);
            const to = parseDateValue(hiddenTo.value);
            const state = {
                mode: 'days',
                from: from && to && from > to ? to : from,
                to: from && to && from > to ? from : to,
                cursor: from || to || new Date()
            };

            const panel = document.createElement('div');
            panel.className = 'absolute left-0 top-[calc(100%+4px)] z-[90] hidden w-[440px] max-w-[calc(100vw-2rem)] rounded border border-gray-200 bg-white text-sm text-gray-600 shadow-xl';
            panel.dataset.ebRangePanel = 'true';
            range.appendChild(panel);

            const refresh = () => {
                hiddenFrom.value = state.from ? toIsoDate(state.from) : '';
                hiddenTo.value = state.to ? toIsoDate(state.to) : '';
                refreshRangeLabel(label, hiddenFrom.value, hiddenTo.value);
                renderRangePicker(panel, state, refresh);
            };

            toggle.addEventListener('click', event => {
                event.stopPropagation();
                closeAllDatePanels(panel);
                closeDropdownPanels();
                state.mode = 'days';
                renderRangePicker(panel, state, refresh);
                panel.classList.toggle('hidden');
            });

            panel.addEventListener('mousedown', event => {
                event.preventDefault();
                event.stopPropagation();
            });
            panel.addEventListener('click', event => {
                event.stopPropagation();
            });

            refreshRangeLabel(label, hiddenFrom.value, hiddenTo.value);
        });
    }

    function renderSinglePicker(panel, state, selectDate) {
        const render = () => renderSinglePicker(panel, state, selectDate);

        if (state.mode === 'months') {
            panel.innerHTML = renderShell({
                title: String(state.cursor.getFullYear()),
                body: `<div class="grid grid-cols-3 gap-y-4 px-12 py-5 text-center">
                    ${shortMonthNames.map((month, index) => `<button type="button" data-month="${index}" class="rounded py-2 hover:bg-sky-50">${month}</button>`).join('')}
                </div>`
            });
        } else if (state.mode === 'years') {
            const year = state.cursor.getFullYear();
            const startYear = Math.floor(year / 10) * 10;
            const years = Array.from({ length: 10 }, (_, index) => startYear + index);
            panel.innerHTML = renderShell({
                title: `${startYear} - ${startYear + 9}`,
                body: `<div class="grid grid-cols-2 gap-y-4 px-24 py-5 text-center">
                    ${years.map(item => `<button type="button" data-year="${item}" class="rounded py-2 hover:bg-sky-50">${item}</button>`).join('')}
                </div>`
            });
        } else {
            panel.innerHTML = renderDayShell(state.cursor, date => {
                const selected = state.selected && sameDate(date, state.selected);
                return {
                    attr: `data-date="${toIsoDate(date)}"`,
                    className: selected ? 'bg-slate-300 text-gray-900' : 'hover:bg-sky-50',
                    textClass: date.getMonth() === state.cursor.getMonth() ? 'text-gray-700' : 'text-gray-400'
                };
            });
        }

        wireSharedPickerActions(panel, state, render, date => selectDate(date), () => selectDate(new Date()), () => {
            state.selected = null;
            const hidden = panel.parentElement.querySelector('[data-date-hidden]');
            const display = panel.parentElement.querySelector('[data-date-display]');
            if (hidden) hidden.value = '';
            if (display) display.value = '';
            hidden?.dispatchEvent(new Event('change', { bubbles: true }));
            panel.classList.add('hidden');
        });
    }

    function renderRangePicker(panel, state, refresh) {
        const render = () => renderRangePicker(panel, state, refresh);

        if (state.mode === 'months') {
            panel.innerHTML = renderShell({
                title: String(state.cursor.getFullYear()),
                body: `<div class="grid grid-cols-3 gap-y-4 px-12 py-5 text-center">
                    ${shortMonthNames.map((month, index) => `<button type="button" data-month="${index}" class="rounded py-2 hover:bg-sky-50">${month}</button>`).join('')}
                </div>`
            });
        } else if (state.mode === 'years') {
            const year = state.cursor.getFullYear();
            const startYear = Math.floor(year / 10) * 10;
            const years = Array.from({ length: 10 }, (_, index) => startYear + index);
            panel.innerHTML = renderShell({
                title: `${startYear} - ${startYear + 9}`,
                body: `<div class="grid grid-cols-2 gap-y-4 px-24 py-5 text-center">
                    ${years.map(item => `<button type="button" data-year="${item}" class="rounded py-2 hover:bg-sky-50">${item}</button>`).join('')}
                </div>`
            });
        } else {
            panel.innerHTML = renderDayShell(state.cursor, date => {
                const inRange = state.from && state.to && date >= stripTime(state.from) && date <= stripTime(state.to);
                const isEdge = (state.from && sameDate(date, state.from)) || (state.to && sameDate(date, state.to));
                const isCurrentMonth = date.getMonth() === state.cursor.getMonth();
                return {
                    attr: `data-date="${toIsoDate(date)}"`,
                    className: isEdge || inRange ? 'bg-sky-100 text-gray-800' : 'hover:bg-sky-50',
                    textClass: isCurrentMonth ? 'text-gray-700' : 'text-gray-400'
                };
            });
        }

        wireSharedPickerActions(panel, state, render, date => {
            if (!state.from || state.to) {
                state.from = date;
                state.to = null;
            } else if (date < state.from) {
                state.to = state.from;
                state.from = date;
            } else {
                state.to = date;
            }

            state.cursor = date;
            refresh();
        }, () => {
            const today = stripTime(new Date());
            state.from = today;
            state.to = today;
            state.cursor = today;
            refresh();
        }, () => {
            state.from = null;
            state.to = null;
            refresh();
            panel.classList.add('hidden');
        });
    }

    function renderDayShell(cursor, getCellState) {
        const year = cursor.getFullYear();
        const month = cursor.getMonth();
        const firstDay = new Date(year, month, 1);
        const start = new Date(year, month, 1 - firstDay.getDay());
        const cells = [];

        for (let i = 0; i < 42; i += 1) {
            const date = new Date(start);
            date.setDate(start.getDate() + i);
            const cell = getCellState(date);
            cells.push(`
                <button type="button" ${cell.attr} class="mx-auto flex h-8 w-8 items-center justify-center rounded-full text-sm ${cell.className} ${cell.textClass}">
                    ${date.getDate()}
                </button>
            `);
        }

        return `
            <div class="flex h-12 items-center justify-between px-4">
                <button type="button" data-nav="prev" class="flex h-8 w-8 items-center justify-center rounded text-2xl text-gray-500 hover:bg-gray-100">&lsaquo;</button>
                <div class="flex items-center gap-3 text-lg font-semibold text-gray-700">
                    <button type="button" data-switch-month class="rounded px-2 py-1 hover:bg-slate-100">${monthNames[month]}</button>
                    <button type="button" data-switch-year class="rounded px-2 py-1 hover:bg-slate-100">${year}</button>
                </div>
                <button type="button" data-nav="next" class="flex h-8 w-8 items-center justify-center rounded text-2xl text-gray-500 hover:bg-gray-100">&rsaquo;</button>
            </div>
            <div class="mx-3 border-t border-gray-200"></div>
            <div class="grid grid-cols-7 px-4 pt-4 text-center text-base font-bold text-gray-700">
                ${weekNames.map(day => `<div>${day}</div>`).join('')}
            </div>
            <div class="grid grid-cols-7 gap-y-3 px-4 py-4 text-center">
                ${cells.join('')}
            </div>
            <div class="flex h-12 items-center justify-between border-t border-gray-200 px-4">
                <button type="button" data-today class="text-sm font-semibold text-sky-700">Today</button>
                <button type="button" data-clear class="text-sm font-semibold text-slate-600">Clear</button>
            </div>
        `;
    }

    function renderShell({ title, body }) {
        return `
            <div class="flex h-12 items-center justify-between border-b border-gray-200 px-4">
                <button type="button" data-nav="prev" class="flex h-8 w-8 items-center justify-center rounded text-2xl text-gray-500 hover:bg-gray-100">&lsaquo;</button>
                <button type="button" data-switch-view class="rounded bg-slate-100 px-3 py-1 text-base font-semibold text-gray-700 hover:bg-slate-200">${title}</button>
                <button type="button" data-nav="next" class="flex h-8 w-8 items-center justify-center rounded text-2xl text-gray-500 hover:bg-gray-100">&rsaquo;</button>
            </div>
            ${body}
            <div class="flex h-12 items-center justify-between border-t border-gray-200 px-5">
                <button type="button" data-today class="font-semibold text-sky-700">Today</button>
                <button type="button" data-clear class="font-semibold text-slate-600">Clear</button>
            </div>
        `;
    }

    function wireSharedPickerActions(panel, state, render, selectDate, selectToday, clear) {
        panel.querySelector('[data-switch-month]')?.addEventListener('click', event => {
            event.stopPropagation();
            state.mode = 'months';
            render();
        });

        panel.querySelector('[data-switch-year]')?.addEventListener('click', event => {
            event.stopPropagation();
            state.mode = 'years';
            render();
        });

        panel.querySelector('[data-switch-view]')?.addEventListener('click', event => {
            event.stopPropagation();
            state.mode = state.mode === 'months' ? 'years' : 'days';
            render();
        });

        panel.querySelectorAll('[data-nav]').forEach(button => {
            button.addEventListener('click', event => {
                event.stopPropagation();
                moveCursor(state, button.dataset.nav === 'prev' ? -1 : 1);
                render();
            });
        });

        panel.querySelectorAll('[data-date]').forEach(button => {
            button.addEventListener('click', event => {
                event.stopPropagation();
                const date = parseDateValue(button.dataset.date);
                if (date) {
                    selectDate(date);
                }
            });
        });

        panel.querySelectorAll('[data-month]').forEach(button => {
            button.addEventListener('click', event => {
                event.stopPropagation();
                state.cursor = new Date(state.cursor.getFullYear(), Number(button.dataset.month), 1);
                state.mode = 'days';
                render();
            });
        });

        panel.querySelectorAll('[data-year]').forEach(button => {
            button.addEventListener('click', event => {
                event.stopPropagation();
                state.cursor = new Date(Number(button.dataset.year), state.cursor.getMonth(), 1);
                state.mode = 'months';
                render();
            });
        });

        panel.querySelector('[data-today]')?.addEventListener('click', event => {
            event.stopPropagation();
            selectToday();
        });

        panel.querySelector('[data-clear]')?.addEventListener('click', event => {
            event.stopPropagation();
            clear();
        });
    }

    function moveCursor(state, direction) {
        if (state.mode === 'days') {
            state.cursor = new Date(state.cursor.getFullYear(), state.cursor.getMonth() + direction, 1);
        } else if (state.mode === 'months') {
            state.cursor = new Date(state.cursor.getFullYear() + direction, state.cursor.getMonth(), 1);
        } else {
            state.cursor = new Date(state.cursor.getFullYear() + direction * 10, state.cursor.getMonth(), 1);
        }
    }

    function refreshRangeLabel(label, from, to) {
        if (!label) {
            return;
        }

        const placeholder = label.dataset.placeholder || '';

        if (from && to) {
            label.textContent = `${formatIsoDisplayDate(from)} - ${formatIsoDisplayDate(to)}`;
        } else if (from) {
            label.textContent = `Từ ${formatIsoDisplayDate(from)}`;
        } else if (to) {
            label.textContent = `Đến ${formatIsoDisplayDate(to)}`;
        } else {
            label.textContent = placeholder;
        }
    }

    function syncTypedDate(display, hidden, state) {
        const typedDate = parseDisplayDate(display.value);

        if (!typedDate) {
            if (!display.value.trim()) {
                hidden.value = '';
                state.selected = null;
            }

            return;
        }

        state.selected = typedDate;
        state.cursor = typedDate;
        hidden.value = toIsoDate(typedDate);
        display.value = formatDisplayDate(typedDate);
        hidden.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function closeAllDatePanels(except) {
        document.querySelectorAll('[data-eb-date-panel], [data-eb-range-panel]').forEach(panel => {
            if (panel !== except) {
                panel.classList.add('hidden');
            }
        });
    }

    function closeDropdownPanels() {
        document.querySelectorAll('[data-dropdown-panel]').forEach(panel => panel.classList.add('hidden'));
    }

    document.addEventListener('click', event => {
        if (!event.target.closest('[data-date-range]') && !event.target.closest('[data-date-display]')) {
            closeAllDatePanels();
        }
    });

    function parseDateValue(value) {
        if (!value) {
            return null;
        }

        const normalizedValue = value.trim();
        const isoMatch = normalizedValue.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/);

        if (isoMatch) {
            return createValidDate(Number(isoMatch[1]), Number(isoMatch[2]), Number(isoMatch[3]));
        }

        const displayDate = parseDisplayDate(normalizedValue);

        if (displayDate) {
            return displayDate;
        }

        const digits = normalizedValue.replace(/\D/g, '');

        if (digits.length === 8) {
            return createValidDate(Number(digits.slice(4, 8)), Number(digits.slice(2, 4)), Number(digits.slice(0, 2)));
        }

        return null;
    }

    function createValidDate(year, month, day) {
        const date = new Date(year, month - 1, day);

        if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) {
            return null;
        }

        return stripTime(date);
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

    function formatIsoDisplayDate(value) {
        const date = parseDateValue(value);
        return date ? formatDisplayDate(date) : value;
    }

    function parseDisplayDate(value) {
        const match = value.trim().match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);

        if (!match) {
            return null;
        }

        return createValidDate(Number(match[3]), Number(match[2]), Number(match[1]));
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

    function stripTime(date) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }
})();
