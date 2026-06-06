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
