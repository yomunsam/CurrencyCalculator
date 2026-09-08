const states = new WeakMap();
const modalDialogs = new Set();
let previousOverflow;

function setModalLock(dialog, modal) {
    if (modal && !modalDialogs.has(dialog)) {
        if (modalDialogs.size === 0) {
            previousOverflow = document.body.style.overflow;
            document.body.style.overflow = 'hidden';
        }
        modalDialogs.add(dialog);
    } else if (!modal && modalDialogs.delete(dialog) && modalDialogs.size === 0) {
        document.body.style.overflow = previousOverflow;
    }
}

function present(dialog, state) {
    // 宽度仅控制面板布局和模态性，不参与输入方式判断。
    if (state.wide.matches) dialog.show();
    else dialog.showModal();
    // 嵌套的图标告知关闭后，底层设置面板仍需阻止背景滚动。
    setModalLock(dialog, !state.wide.matches);
}

export function initialize(dialog, reference) {
    const controller = new AbortController();
    const options = { signal: controller.signal };
    const dismiss = () => reference.invokeMethodAsync('DismissAsync');
    const state = { controller, wide: matchMedia('(min-width: 640px)') };
    state.wide.addEventListener('change', () => {
        if (!dialog.open) return;
        const focused = document.activeElement;
        dialog.close();
        present(dialog, state);
        if (dialog.contains(focused)) focused.focus({ preventScroll: true });
    }, options);
    dialog.addEventListener('cancel', event => {
        event.preventDefault();
        dismiss();
    }, options);
    dialog.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            dismiss();
        }
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
    states.set(dialog, state);
}
export function open(dialog) {
    const state = states.get(dialog);
    state.opener = document.activeElement;
    dialog.querySelector('.panel-close').setAttribute('autofocus', '');
    present(dialog, state);
}
export function close(dialog) {
    if (!dialog.open) return;
    const restoreFocus = dialog.contains(document.activeElement);
    dialog.close();
    setModalLock(dialog, false);
    const opener = states.get(dialog).opener;
    // 点击左侧金额离开宽屏面板时，不能把焦点从新输入框抢回。
    if (restoreFocus && !document.querySelector('dialog[open]') && opener?.isConnected) {
        opener.focus({ preventScroll: true });
    }
}
export function dispose(dialog) {
    close(dialog);
    states.get(dialog)?.controller.abort();
    states.delete(dialog);
}
