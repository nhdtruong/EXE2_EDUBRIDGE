(() => {
    const config = window.classEnrollmentConfig;
    const modal = document.getElementById('enrollStudentsModal');
    const search = document.getElementById('availableStudentSearch');
    const list = document.getElementById('availableStudentsList');
    const error = document.getElementById('enrollStudentsError');
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const removeModal = document.getElementById('removeStudentModal');
    const removeName = document.getElementById('removeStudentName');
    const removeError = document.getElementById('removeStudentError');
    const confirmRemove = document.getElementById('confirmRemoveStudent');
    if (!config || !modal || !search || !list) return;

    let timer;
    let pendingRemoveStudentId = null;
    const showError = message => {
        error.textContent = message || '';
        error.classList.toggle('hidden', !message);
    };
    const headers = () => ({
        'Content-Type': 'application/json',
        'RequestVerificationToken': tokenInput?.value || ''
    });
    const open = () => {
        modal.classList.remove('hidden');
        modal.classList.add('flex');
        search.focus();
        loadAvailable('');
    };
    const close = () => {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
        showError('');
    };
    const loadAvailable = async keyword => {
        list.innerHTML = '<div class="p-6 text-center text-sm text-gray-500">Đang tải...</div>';
        const response = await fetch(`?handler=AvailableStudents&classId=${config.classId}&keyword=${encodeURIComponent(keyword)}`);
        const payload = await response.json();
        if (!response.ok || !payload.success) {
            list.innerHTML = `<div class="p-6 text-center text-sm text-red-600">${payload.message || 'Không thể tải dữ liệu.'}</div>`;
            return;
        }
        if (!payload.data?.length) {
            list.innerHTML = '<div class="p-6 text-center text-sm text-gray-500">Không tìm thấy học sinh phù hợp.</div>';
            return;
        }
        list.innerHTML = payload.data.map(student => `
            <label class="flex cursor-pointer items-center gap-3 p-4 hover:bg-blue-50">
                <input type="checkbox" value="${student.studentId}" class="available-student h-4 w-4">
                <span class="min-w-0">
                    <strong class="block text-sm text-gray-900">${escapeHtml(student.fullName)}</strong>
                    <span class="text-xs text-gray-500">${escapeHtml(student.studentCode)}${student.phoneNumber ? ` - ${escapeHtml(student.phoneNumber)}` : ''}</span>
                </span>
            </label>`).join('');
    };
    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, char => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;'
    })[char]);

    document.getElementById('openEnrollStudentsModal')?.addEventListener('click', open);
    document.querySelectorAll('[data-close-enroll-modal]').forEach(button => button.addEventListener('click', close));
    search.addEventListener('input', () => {
        clearTimeout(timer);
        timer = setTimeout(() => loadAvailable(search.value.trim()), 300);
    });
    document.getElementById('saveEnrolledStudents')?.addEventListener('click', async () => {
        const studentIds = [...document.querySelectorAll('.available-student:checked')].map(item => Number(item.value));
        if (!studentIds.length) return showError('Vui lòng chọn ít nhất một học sinh.');
        const response = await fetch(`?handler=EnrollStudents&classId=${config.classId}`, {
            method: 'POST', headers: headers(), body: JSON.stringify({ studentIds })
        });
        const payload = await response.json();
        if (!response.ok || !payload.success) return showError(payload.message || 'Không thể thêm học sinh.');
        window.location.reload();
    });
    const closeRemoveModal = () => {
        removeModal?.classList.add('hidden');
        removeModal?.classList.remove('flex');
        pendingRemoveStudentId = null;
        if (removeError) {
            removeError.textContent = '';
            removeError.classList.add('hidden');
        }
    };
    document.querySelectorAll('[data-close-remove-modal]').forEach(button => button.addEventListener('click', closeRemoveModal));
    removeModal?.addEventListener('click', event => {
        if (event.target === removeModal) closeRemoveModal();
    });
    document.querySelectorAll('[data-remove-student]').forEach(button => button.addEventListener('click', () => {
        pendingRemoveStudentId = Number(button.dataset.removeStudent);
        if (removeName) removeName.textContent = button.dataset.studentName || 'học sinh này';
        removeModal?.classList.remove('hidden');
        removeModal?.classList.add('flex');
    }));
    confirmRemove?.addEventListener('click', async () => {
        if (!pendingRemoveStudentId) return;
        confirmRemove.disabled = true;
        const response = await fetch(`?handler=RemoveStudent&classId=${config.classId}&studentId=${pendingRemoveStudentId}`, {
            method: 'POST', headers: headers(), body: '{}'
        });
        const payload = await response.json();
        confirmRemove.disabled = false;
        if (!response.ok || !payload.success) {
            if (removeError) {
                removeError.textContent = payload.message || 'Không thể đưa học sinh ra khỏi lớp.';
                removeError.classList.remove('hidden');
            }
            return;
        }
        window.location.reload();
    });
})();
