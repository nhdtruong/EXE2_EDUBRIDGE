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

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-multi-select-dropdown]').forEach(dropdown => {
        const toggleBtn = dropdown.querySelector('[data-dropdown-toggle]');
        const panel = dropdown.querySelector('[data-dropdown-panel]');
        const searchInput = dropdown.querySelector('[data-search-input]');
        const textSpan = dropdown.querySelector('[data-dropdown-text]');
        const checkboxes = dropdown.querySelectorAll('input[type="checkbox"]');

        const updateText = () => {
            const selected = Array.from(checkboxes).filter(cb => cb.checked).map(cb => cb.nextElementSibling.textContent.trim());
            if (selected.length === 0) {
                textSpan.innerHTML = 'Chọn vai trò...';
                textSpan.classList.add('text-gray-400');
                textSpan.classList.remove('text-gray-700');
            } else {
                textSpan.innerHTML = selected.map(role => `<span class="inline-flex items-center rounded border border-blue-200 bg-blue-50 px-2 py-0.5 text-[11px] font-medium text-blue-700">${role}</span>`).join('<span class="mx-0.5"></span>');
                textSpan.classList.remove('text-gray-400');
                textSpan.classList.add('text-gray-700');
            }
        };

        updateText();

        checkboxes.forEach(cb => {
            cb.addEventListener('change', updateText);
        });

        toggleBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            panel.classList.toggle('hidden');
            if (!panel.classList.contains('hidden')) {
                searchInput?.focus();
            }
        });

        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                const term = e.target.value.toLowerCase();
                dropdown.querySelectorAll('[data-searchable]').forEach(item => {
                    const text = item.querySelector('[data-search-text]').textContent.toLowerCase();
                    item.style.display = text.includes(term) ? '' : 'none';
                });
            });
            searchInput.addEventListener('click', (e) => e.stopPropagation());
        }

        panel.addEventListener('click', (e) => e.stopPropagation());
    });

    document.addEventListener('click', () => {
        document.querySelectorAll('[data-multi-select-dropdown] [data-dropdown-panel]').forEach(panel => {
            panel.classList.add('hidden');
        });
    });
});

