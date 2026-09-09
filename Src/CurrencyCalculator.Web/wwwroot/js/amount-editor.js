import { editSelection } from './amount-editing.js';

let state;
const isTouch = type => type === 'touch' || type === 'pen';

function hide() {
    state.keyboard.hidden = true;
    document.documentElement.style.removeProperty('--keyboard-space');
}

function configure(input) {
    const custom = isTouch(state.pointer) && !state.system && state.keyboard.dataset.never !== 'true';
    input.inputMode = custom ? 'none' : 'text';
    return custom;
}

function show(input) {
    state.input = input;
    if (!configure(input)) return hide();
    state.keyboard.hidden = false;
    document.documentElement.style.setProperty('--keyboard-space', `${state.keyboard.offsetHeight + 12}px`);
    input.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
}

export function focusAmount(input) {
    if (!state || !input) return;
    configure(input);
    input.focus({ preventScroll: true });
    input.setSelectionRange(input.value.length, input.value.length);
    show(input);
}

export function initialize(keyboard) {
    const controller = new AbortController();
    const options = { signal: controller.signal };
    state = { keyboard, controller, pointer: 'keyboard', system: false, input: null };
    // 捕获阶段先设置 inputmode，必须早于浏览器默认聚焦行为。
    document.addEventListener('pointerdown', event => {
        if (event.isPrimary === false || event.button > 0) return;
        if (keyboard.contains(event.target)) {
            // 包括键间空隙和面板留白，避免默认行为让金额框失焦并收起键盘。
            event.preventDefault();
            return;
        }
        const returningFromSystem = state.system && isTouch(event.pointerType);
        state.system = false;
        state.pointer = event.pointerType;
        const input = event.target.closest('.amount-input');
        if (input) {
            const custom = configure(input);
            if (returningFromSystem && custom && document.activeElement === input) {
                // 已聚焦的原生输入需要重新聚焦，才能让系统键盘收起。
                const start = input.selectionStart;
                const end = input.selectionEnd;
                input.blur();
                input.focus({ preventScroll: true });
                input.setSelectionRange(start, end);
            }
            if (document.activeElement === input) show(input);
        } else if (!event.target.closest('.amount-label, .row-clear')) {
            hide();
        }
    }, { ...options, capture: true });
    document.addEventListener('focusin', event => {
        if (event.target.matches('.amount-input')) show(event.target);
        else if (!keyboard.contains(event.target)) hide();
    }, options);
    document.addEventListener('focusout', () => {
        queueMicrotask(() => {
            if (!state) return;
            if (!document.activeElement?.matches('.amount-input') && !keyboard.contains(document.activeElement)) hide();
        });
    }, options);
    document.addEventListener('keydown', event => {
        if (event.key === 'Tab') {
            state.pointer = 'keyboard';
        }
        if (event.key === 'Enter' && event.isComposing && event.target.matches('.amount-input')) {
            event.stopPropagation();
        }
        if (event.key === 'Escape' && !keyboard.hidden) {
            event.preventDefault();
            hide();
        }
    }, { ...options, capture: true });
    keyboard.addEventListener('click', event => {
        const key = event.target.closest('[data-key]')?.dataset.key;
        const input = state.input;
        if (!key || !input?.isConnected) return;
        if (key === 'hide') {
            hide();
            input.blur();
            return;
        }
        if (key === 'system') {
            state.system = true;
            hide();
            const start = input.selectionStart;
            const end = input.selectionEnd;
            input.inputMode = 'text';
            // 同一用户点击内重新聚焦，允许浏览器唤起系统键盘。
            input.blur();
            input.focus({ preventScroll: true });
            input.setSelectionRange(start, end);
            return;
        }
        if (key === '=') {
            input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
            return;
        }
        const edited = editSelection(input.value, input.selectionStart, input.selectionEnd, key);
        input.value = edited.value;
        input.setSelectionRange(edited.caret, edited.caret);
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }, options);
    const observer = new MutationObserver(() => {
        state.system = false;
        if (state.input?.isConnected) configure(state.input);
        hide();
    });
    observer.observe(keyboard, { attributes: true, attributeFilter: ['data-never'] });
    state.observer = observer;
    const resizeObserver = new ResizeObserver(() => {
        if (!keyboard.hidden) {
            document.documentElement.style.setProperty('--keyboard-space', `${keyboard.offsetHeight + 12}px`);
        }
    });
    resizeObserver.observe(keyboard);
    state.resizeObserver = resizeObserver;
}

export function dispose() {
    if (!state) return;
    hide();
    state.controller.abort();
    state.observer.disconnect();
    state.resizeObserver.disconnect();
    state = null;
}
