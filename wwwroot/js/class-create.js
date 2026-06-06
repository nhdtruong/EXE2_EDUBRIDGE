(() => {
    const rowsContainer = document.getElementById('scheduleRows');
    const rowTemplate = document.getElementById('scheduleRowTemplate');
    const courseSelect = document.getElementById('classCourse');
    const totalSessionsInput = document.getElementById('classTotalSessions');
    const startDateInput = document.getElementById('Input_StartDate');
    const endDatePreview = document.getElementById('classEndDatePreview');

    if (!rowsContainer || !rowTemplate) {
        return;
    }

    const reindexRows = () => {
        rowsContainer.querySelectorAll('.schedule-row').forEach((row, index) => {
            row.querySelectorAll('[name], [data-field]').forEach(field => {
                const fieldName = field.dataset.field || field.name.split('.').pop();
                field.name = `Input.Schedules[${index}].${fieldName}`;
            });
        });
    };

    const updateEndDate = () => {
        const totalSessions = Number.parseInt(totalSessionsInput?.value || '', 10);
        const startDate = startDateInput?.value;
        const schedules = [...rowsContainer.querySelectorAll('.schedule-row')]
            .map(row => Number.parseInt(row.querySelector('.schedule-day')?.value || '0', 10))
            .filter(day => day >= 1 && day <= 7);

        if (!startDate || !Number.isFinite(totalSessions) || totalSessions < 1 || schedules.length === 0) {
            endDatePreview.value = '';
            return;
        }

        const allowedDays = new Set(schedules);
        const current = new Date(`${startDate}T00:00:00`);
        let remaining = totalSessions;
        let guard = 0;

        while (remaining > 0 && guard < totalSessions * 14 + 14) {
            const jsDay = current.getDay();
            const day = jsDay === 0 ? 7 : jsDay;
            const dailySessions = schedules.filter(item => item === day).length;
            remaining -= Math.min(remaining, dailySessions);

            if (remaining > 0) {
                current.setDate(current.getDate() + 1);
            }

            guard += 1;
        }

        endDatePreview.value = guard > 0
            ? current.toLocaleDateString('vi-VN')
            : '';
    };

    const bindRow = row => {
        row.querySelector('.schedule-shift')?.addEventListener('change', event => {
            const option = event.target.selectedOptions[0];

            if (option?.dataset.start && option?.dataset.end) {
                row.querySelector('.schedule-start').value = option.dataset.start;
                row.querySelector('.schedule-end').value = option.dataset.end;
            }

            updateEndDate();
        });

        row.querySelectorAll('.schedule-day, .schedule-start, .schedule-end')
            .forEach(field => field.addEventListener('change', updateEndDate));

        row.querySelector('.remove-schedule')?.addEventListener('click', () => {
            if (rowsContainer.querySelectorAll('.schedule-row').length === 1) {
                row.querySelectorAll('select, input').forEach(field => {
                    field.value = field.classList.contains('schedule-day') ? '0' : '';
                });
            } else {
                row.remove();
            }

            reindexRows();
            updateEndDate();
        });
    };

    document.getElementById('addScheduleButton')?.addEventListener('click', () => {
        const row = rowTemplate.content.firstElementChild.cloneNode(true);
        rowsContainer.appendChild(row);
        bindRow(row);
        reindexRows();
    });

    courseSelect?.addEventListener('change', () => {
        const sessions = courseSelect.selectedOptions[0]?.dataset.totalSessions;

        if (sessions) {
            totalSessionsInput.value = sessions;
        }

        updateEndDate();
    });

    totalSessionsInput?.addEventListener('input', updateEndDate);
    startDateInput?.addEventListener('change', updateEndDate);
    document.addEventListener('date-picker:change', updateEndDate);

    rowsContainer.querySelectorAll('.schedule-row').forEach(bindRow);
    reindexRows();
    updateEndDate();
})();
