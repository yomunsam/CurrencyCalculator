import test from 'node:test';
import assert from 'node:assert/strict';
import { editSelection } from '../../Src/CurrencyCalculator.Web/wwwroot/js/amount-editing.js';
import { initialize, focusAmount, dispose } from '../../Src/CurrencyCalculator.Web/wwwroot/js/amount-editor.js';

test('中间插入、选区替换、退格和清空保留正确光标', () => {
    assert.deepEqual(editSelection('12+5', 2, 2, '*3'), { value: '12*3+5', caret: 4 });
    assert.deepEqual(editSelection('12+5', 0, 2, '9'), { value: '9+5', caret: 1 });
    assert.deepEqual(editSelection('12+5', 2, 2, 'backspace'), { value: '1+5', caret: 1 });
    assert.deepEqual(editSelection('12+5', 0, 2, 'backspace'), { value: '+5', caret: 0 });
    assert.deepEqual(editSelection('12', 0, 0, 'backspace'), { value: '12', caret: 0 });
    assert.deepEqual(editSelection('12', 1, 1, 'clear'), { value: '', caret: 0 });
});

// 模拟 DOM 事件边界，不声称覆盖浏览器实际键盘唤起、滚动或触摸选区。
class Surface {
    constructor(className = '', parent = null) {
        this.className = className;
        this.parent = parent;
        this.listeners = new Map();
        this.dataset = {};
        this.isConnected = true;
    }
    addEventListener(name, callback) {
        const callbacks = this.listeners.get(name) ?? [];
        callbacks.push(callback);
        this.listeners.set(name, callbacks);
    }
    emit(name, props = {}) {
        const event = { target: this, preventDefault() {}, stopPropagation() {}, ...props };
        for (const callback of this.listeners.get(name) ?? []) callback(event);
    }
    matches(selector) { return selector.split(', ').includes(`.${this.className}`); }
    closest(selector) {
        if (this.matches(selector) || (selector === '[data-key]' && this.dataset.key)) return this;
        if (selector === 'button' && this.dataset.key) return this;
        return this.parent?.closest(selector) ?? null;
    }
    contains(target) { return target === this || Boolean(target?.parent && this.contains(target.parent)); }
    focus() { document.activeElement = this; document.emit('focusin', { target: this }); }
    blur() { document.activeElement = null; document.emit('focusout', { target: this }); }
    setSelectionRange(start, end) { this.selectionStart = start; this.selectionEnd = end; }
    scrollIntoView() {}
    dispatchEvent(event) { this.events.push(event); }
}

function fixture(t, never = false) {
    globalThis.document = new Surface();
    document.documentElement = { dataset: {}, style: { setProperty() {}, removeProperty() {} } };
    let onPreference;
    globalThis.MutationObserver = class {
        constructor(callback) { onPreference = callback; }
        observe() {}
        disconnect() {}
    };
    globalThis.ResizeObserver = class { observe() {} disconnect() {} };
    globalThis.KeyboardEvent = class extends Event {
        constructor(type, options) { super(type, options); this.key = options.key; }
    };
    const keyboard = new Surface();
    keyboard.dataset.never = String(never);
    keyboard.hidden = true;
    keyboard.offsetHeight = 330;
    const input = new Surface('amount-input');
    input.value = '12+5';
    input.events = [];
    input.setSelectionRange(4, 4);
    initialize(keyboard);
    t.after(dispose);
    return {
        keyboard, input,
        pointer(type, target = input) { document.emit('pointerdown', { target, pointerType: type }); },
        key(value) {
            const target = new Surface('', keyboard);
            target.dataset.key = value;
            keyboard.emit('click', { target });
        },
        preference(value) { keyboard.dataset.never = String(value); onPreference(); }
    };
}

for (const type of ['touch', 'pen']) {
    test(`${type} 在聚焦前禁用系统输入模式，鼠标和 Tab 可切回原生输入`, t => {
        const f = fixture(t);
        f.pointer(type);
        assert.equal(f.input.inputMode, 'none');
        f.input.focus();
        assert.equal(f.keyboard.hidden, false);
        f.pointer('mouse');
        assert.equal(f.input.inputMode, 'text');
        assert.equal(f.keyboard.hidden, true);
        f.pointer(type);
        document.emit('keydown', { key: 'Tab' });
        f.input.focus();
        assert.equal(f.input.inputMode, 'text');
        assert.equal(f.keyboard.hidden, true);
    });
}

test('非活动标签激活为输入框后沿用实际触控类型', t => {
    const f = fixture(t);
    f.pointer('pen', new Surface('amount-label'));
    focusAmount(f.input);
    assert.equal(f.input.inputMode, 'none');
    assert.equal(f.keyboard.hidden, false);
    assert.equal(f.input.selectionStart, 4);
});

test('内置键盘编辑当前选区，等号只发 Enter，不追加结果文本', t => {
    const f = fixture(t);
    f.pointer('touch');
    f.input.focus();
    f.input.setSelectionRange(0, 2);
    f.key('7');
    assert.equal(f.input.value, '7+5');
    assert.equal(f.input.selectionStart, 1);
    assert.equal(f.input.events.at(-1).type, 'input');
    f.key('=');
    assert.equal(f.input.value, '7+5');
    assert.equal(f.input.events.at(-1).key, 'Enter');
});

test('系统键盘切换保留文本选区，下次笔点击重新打开内置键盘', t => {
    const f = fixture(t);
    f.pointer('touch');
    f.input.focus();
    f.input.setSelectionRange(1, 3);
    f.key('system');
    assert.equal(f.input.value, '12+5');
    assert.equal(f.input.selectionStart, 1);
    assert.equal(f.input.selectionEnd, 3);
    assert.equal(f.input.inputMode, 'text');
    f.pointer('pen');
    assert.equal(f.keyboard.hidden, false);
    assert.equal(f.input.inputMode, 'none');
    assert.equal(f.input.selectionStart, 1);
    assert.equal(f.input.selectionEnd, 3);
});

test('永不使用设置压过触控事件，切换设置立即关闭内置键盘', t => {
    const f = fixture(t, true);
    f.pointer('touch');
    f.input.focus();
    assert.equal(f.input.inputMode, 'text');
    assert.equal(f.keyboard.hidden, true);
    f.preference(false);
    f.pointer('pen');
    assert.equal(f.keyboard.hidden, false);
    f.preference(true);
    assert.equal(f.keyboard.hidden, true);
    assert.equal(f.input.inputMode, 'text');
});

test('点击键盘留白和键间区域阻止默认失焦', t => {
    const f = fixture(t);
    f.pointer('touch');
    f.input.focus();
    let prevented = false;
    document.emit('pointerdown', { target: new Surface('', f.keyboard), pointerType: 'touch', preventDefault() { prevented = true; } });
    assert.equal(prevented, true);
    assert.equal(f.keyboard.hidden, false);
    assert.equal(document.activeElement, f.input);
});

for (const type of ['touch', 'pen']) {
    test(`系统键盘之后 ${type} 点击另一金额也打开内置键盘`, t => {
        const f = fixture(t);
        f.pointer(type);
        f.input.focus();
        f.key('system');
        f.pointer(type, new Surface('amount-label'));
        focusAmount(f.input);
        assert.equal(f.keyboard.hidden, false);
        assert.equal(f.input.inputMode, 'none');
    });
}

test('永不使用设置在系统键盘切换后仍压过反复触控和笔点击', t => {
    const f = fixture(t);
    f.pointer('touch');
    f.input.focus();
    f.key('system');
    f.preference(true);
    for (const type of ['touch', 'pen']) {
        f.pointer(type);
        assert.equal(f.keyboard.hidden, true);
        assert.equal(f.input.inputMode, 'text');
    }
});
