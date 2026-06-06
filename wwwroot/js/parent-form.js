document.querySelectorAll('[data-parent-status-toggle]').forEach(button => {
    const container = button.parentElement;
    const value = container.querySelector('[data-parent-status-value]');
    const track = button.querySelector('[data-parent-status-track]');
    const knob = button.querySelector('[data-parent-status-knob]');
    const label = button.querySelector('[data-parent-status-label]');

    const render = () => {
        const isActive = value.value !== 'Inactive';
        value.value = isActive ? 'Active' : 'Inactive';
        button.classList.toggle('border-green-200', isActive);
        button.classList.toggle('bg-green-50', isActive);
        button.classList.toggle('text-green-700', isActive);
        button.classList.toggle('border-gray-200', !isActive);
        button.classList.toggle('bg-gray-100', !isActive);
        button.classList.toggle('text-gray-600', !isActive);
        track.classList.toggle('bg-green-500', isActive);
        track.classList.toggle('bg-gray-300', !isActive);
        knob.classList.toggle('left-5', isActive);
        knob.classList.toggle('left-0.5', !isActive);
        label.textContent = isActive ? 'Đang hoạt động' : 'Tạm dừng';
    };

    button.addEventListener('click', () => {
        value.value = value.value === 'Active' ? 'Inactive' : 'Active';
        render();
    });

    render();
});
