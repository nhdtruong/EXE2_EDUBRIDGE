// EduBridge exported datepicker for MMO_System.
// No framework dependency. Display value: dd/mm/yyyy. Submitted hidden value: yyyy-MM-dd.
(function initDsDatePicker() {
    const months = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
    ];
    const shortMonths = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const weekdays = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

    document.addEventListener('DOMContentLoaded', initAll);

    function initAll() {
        document.querySelectorAll('[data-ds-datepicker]').forEach(root => {
            if (root.dataset.dsReady === 'true') {
                return;
            }

            const display = root.querySelector('[data-ds-date-display]');
            const hidden = root.querySelector('[data-ds-date-value]');
            const toggle = root.querySelector('[data-ds-date-toggle]');

            if (!display || !hidden || !toggle) {
                return;
            }

            root.dataset.dsReady = 'true';

            const initial = parseDate(hidden.value || display.value);
            const state = {
                mode: 'days',
                selected: initial,
                cursor: initial || new Date()
            };

            if (initial) {
                hidden.value = toIso(initial);
                display.value = toDisplay(initial);
            }

            const panel = document.createElement('div');
            panel.className = 'ds-datepicker-panel';
            panel.hidden = true;
            root.appendChild(panel);

            const selectDate = date => {
                state.selected = stripTime(date);
                state.cursor = stripTime(date);
                hidden.value = toIso(state.selected);
                display.value = toDisplay(state.selected);
                hidden.dispatchEvent(new Event('change', { bubbles: true }));
                display.dispatchEvent(new Event('change', { bubbles: true }));
                panel.hidden = true;
            };

            const open = () => {
                closeAll(panel);
                state.mode = 'days';
                render(panel, state, selectDate);
                panel.hidden = false;
            };

            display.addEventListener('input', () => {
                display.value = formatTyped(display.value);
            });

            display.addEventListener('blur', () => {
                syncTyped(display, hidden, state);
            });

            display.addEventListener('change', () => {
                syncTyped(display, hidden, state);
            });

            display.addEventListener('click', open);
            toggle.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                open();
            });

            panel.addEventListener('mousedown', event => {
                event.preventDefault();
                event.stopPropagation();
            });

            panel.addEventListener('click', event => {
                event.stopPropagation();
            });
        });
    }

    function render(panel, state, selectDate) {
        if (state.mode === 'months') {
            panel.innerHTML = renderShell(
                String(state.cursor.getFullYear()),
                `<div class="ds-datepicker-months">
                    ${shortMonths.map((month, index) => `<button type="button" class="ds-datepicker-cell" data-month="${index}">${month}</button>`).join('')}
                </div>`
            );
        } else if (state.mode === 'years') {
            const year = state.cursor.getFullYear();
            const startYear = Math.floor(year / 10) * 10;
            const years = Array.from({ length: 10 }, (_, index) => startYear + index);
            panel.innerHTML = renderShell(
                `${startYear} - ${startYear + 9}`,
                `<div class="ds-datepicker-years">
                    ${years.map(item => `<button type="button" class="ds-datepicker-cell" data-year="${item}">${item}</button>`).join('')}
                </div>`
            );
        } else {
            panel.innerHTML = renderDays(state);
        }

        wire(panel, state, selectDate);
    }

    function renderDays(state) {
        const year = state.cursor.getFullYear();
        const month = state.cursor.getMonth();
        const first = new Date(year, month, 1);
        const start = new Date(year, month, 1 - first.getDay());
        const cells = [];

        for (let index = 0; index < 42; index += 1) {
            const date = new Date(start);
            date.setDate(start.getDate() + index);
            const isCurrentMonth = date.getMonth() === month;
            const isSelected = state.selected && sameDate(date, state.selected);
            const classes = [
                'ds-datepicker-cell',
                isCurrentMonth ? '' : 'ds-datepicker-cell-muted',
                isSelected ? 'ds-datepicker-cell-selected' : ''
            ].filter(Boolean).join(' ');

            cells.push(`<button type="button" class="${classes}" data-date="${toIso(date)}">${date.getDate()}</button>`);
        }

        return `
            <div class="ds-datepicker-header">
                <button type="button" class="ds-datepicker-nav" data-nav="prev">&lsaquo;</button>
                <div>
                    <button type="button" class="ds-datepicker-title" data-switch-month>${months[month]}</button>
                    <button type="button" class="ds-datepicker-title" data-switch-year>${year}</button>
                </div>
                <button type="button" class="ds-datepicker-nav" data-nav="next">&rsaquo;</button>
            </div>
            <div class="ds-datepicker-weekdays">${weekdays.map(day => `<div>${day}</div>`).join('')}</div>
            <div class="ds-datepicker-days">${cells.join('')}</div>
            <div class="ds-datepicker-footer">
                <button type="button" class="ds-datepicker-footer-button" data-today>Today</button>
                <button type="button" class="ds-datepicker-footer-button" data-clear>Clear</button>
            </div>
        `;
    }

    function renderShell(title, body) {
        return `
            <div class="ds-datepicker-header">
                <button type="button" class="ds-datepicker-nav" data-nav="prev">&lsaquo;</button>
                <button type="button" class="ds-datepicker-title" data-switch-view>${title}</button>
                <button type="button" class="ds-datepicker-nav" data-nav="next">&rsaquo;</button>
            </div>
            ${body}
            <div class="ds-datepicker-footer">
                <button type="button" class="ds-datepicker-footer-button" data-today>Today</button>
                <button type="button" class="ds-datepicker-footer-button" data-clear>Clear</button>
            </div>
        `;
    }

    function wire(panel, state, selectDate) {
        const redraw = () => render(panel, state, selectDate);

        panel.querySelector('[data-switch-month]')?.addEventListener('click', () => {
            state.mode = 'months';
            redraw();
        });

        panel.querySelector('[data-switch-year]')?.addEventListener('click', () => {
            state.mode = 'years';
            redraw();
        });

        panel.querySelector('[data-switch-view]')?.addEventListener('click', () => {
            state.mode = state.mode === 'months' ? 'years' : 'days';
            redraw();
        });

        panel.querySelectorAll('[data-nav]').forEach(button => {
            button.addEventListener('click', () => {
                moveCursor(state, button.dataset.nav === 'prev' ? -1 : 1);
                redraw();
            });
        });

        panel.querySelectorAll('[data-date]').forEach(button => {
            button.addEventListener('click', () => {
                const date = parseDate(button.dataset.date);
                if (date) {
                    selectDate(date);
                }
            });
        });

        panel.querySelectorAll('[data-month]').forEach(button => {
            button.addEventListener('click', () => {
                state.cursor = new Date(state.cursor.getFullYear(), Number(button.dataset.month), 1);
                state.mode = 'days';
                redraw();
            });
        });

        panel.querySelectorAll('[data-year]').forEach(button => {
            button.addEventListener('click', () => {
                state.cursor = new Date(Number(button.dataset.year), state.cursor.getMonth(), 1);
                state.mode = 'months';
                redraw();
            });
        });

        panel.querySelector('[data-today]')?.addEventListener('click', () => selectDate(new Date()));

        panel.querySelector('[data-clear]')?.addEventListener('click', () => {
            const root = panel.closest('[data-ds-datepicker]');
            const display = root?.querySelector('[data-ds-date-display]');
            const hidden = root?.querySelector('[data-ds-date-value]');
            state.selected = null;
            if (display) display.value = '';
            if (hidden) {
                hidden.value = '';
                hidden.dispatchEvent(new Event('change', { bubbles: true }));
            }
            panel.hidden = true;
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

    function syncTyped(display, hidden, state) {
        const typedDate = parseDate(display.value);

        if (!typedDate) {
            if (!display.value.trim()) {
                hidden.value = '';
                state.selected = null;
                hidden.dispatchEvent(new Event('change', { bubbles: true }));
            }
            return;
        }

        state.selected = typedDate;
        state.cursor = typedDate;
        hidden.value = toIso(typedDate);
        display.value = toDisplay(typedDate);
        hidden.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function parseDate(value) {
        if (!value) return null;

        const raw = value.trim();
        const iso = raw.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/);
        if (iso) {
            return validDate(Number(iso[1]), Number(iso[2]), Number(iso[3]));
        }

        const display = raw.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
        if (display) {
            return validDate(Number(display[3]), Number(display[2]), Number(display[1]));
        }

        const digits = raw.replace(/\D/g, '');
        if (digits.length === 8) {
            return validDate(Number(digits.slice(4, 8)), Number(digits.slice(2, 4)), Number(digits.slice(0, 2)));
        }

        return null;
    }

    function validDate(year, month, day) {
        const date = new Date(year, month - 1, day);
        if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) {
            return null;
        }
        return stripTime(date);
    }

    function formatTyped(value) {
        const digits = value.replace(/\D/g, '').slice(0, 8);
        if (digits.length <= 2) return digits;
        if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`;
        return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
    }

    function toIso(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function toDisplay(date) {
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        return `${day}/${month}/${date.getFullYear()}`;
    }

    function sameDate(left, right) {
        return left.getFullYear() === right.getFullYear()
            && left.getMonth() === right.getMonth()
            && left.getDate() === right.getDate();
    }

    function stripTime(date) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    function closeAll(except) {
        document.querySelectorAll('.ds-datepicker-panel').forEach(panel => {
            if (panel !== except) {
                panel.hidden = true;
            }
        });
    }

    document.addEventListener('click', event => {
        if (!event.target.closest('[data-ds-datepicker]')) {
            closeAll();
        }
    });
})();

