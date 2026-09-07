const states = new WeakMap();

export function initialize(dialog, reference) {
    const controller = new AbortController();
    const options = { signal: controller.signal };
    const dismiss = () => reference.invokeMethodAsync('DismissAsync');
    dialog.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            dismiss();
        }
    }, options);
    dialog.addEventListener('cancel', event => {
        event.preventDefault();
        dismiss();
    }, options);
    let backdropPressed = false;
    const outside = event => {
        const bounds = dialog.getBoundingClientRect();
        return event.clientX < bounds.left || event.clientX > bounds.right
            || event.clientY < bounds.top || event.clientY > bounds.bottom;
    };
    dialog.addEventListener('pointerdown', event => {
        backdropPressed = event.target === dialog && outside(event);
    }, options);
    dialog.addEventListener('click', event => {
        if (backdropPressed && event.target === dialog && outside(event)) dismiss();
        backdropPressed = false;
    }, options);
    states.set(dialog, { controller });
}

export function open(dialog) {
    const state = states.get(dialog);
    state.opener = document.activeElement;
    state.overflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    // 先聚焦关闭按钮，触控打开时不会立即唤起系统键盘。
    dialog.querySelector('.selector-close').setAttribute('autofocus', '');
    dialog.showModal();
}

export function close(dialog) {
    const state = states.get(dialog);
    if (!dialog.open) return;
    dialog.close();
    document.body.style.overflow = state.overflow;
    if (state.opener?.isConnected) state.opener.focus({ preventScroll: true });
}

export function dispose(dialog) {
    close(dialog);
    states.get(dialog)?.controller.abort();
    states.delete(dialog);
}
