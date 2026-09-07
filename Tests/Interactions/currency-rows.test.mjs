import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize, dispose } from '../../Src/CurrencyCalculator.Web/wwwroot/js/currency-rows.js';

// 只模拟手势依赖的 DOM 边界，验证取消、指针隔离与提交规则；不替代浏览器触控验收。
class Element {
    constructor(className = '', parent = null, top = 0, tag = 'div') {
        this.classes = new Set(className.split(' '));
        this.classList = {
            add: (...names) => names.forEach(name => this.classes.add(name)),
            remove: (...names) => names.forEach(name => this.classes.delete(name)),
            contains: name => this.classes.has(name),
            toggle: (name, value) => value ? this.classes.add(name) : this.classes.delete(name)
        };
        this.parent = parent;
        this.children = [];
        parent?.children.push(this);
        this.tag = tag;
        this.top = top;
        this.style = {};
        this.dataset = {};
        this.attributes = {};
        this.listeners = {};
        this.captured = new Set();
        this.open = false;
    }
    matches(selector) {
        return selector.split(',').some(part => {
            const value = part.trim().replace(':not(:disabled)', '');
            const separator = value.lastIndexOf(' ');
            if (separator >= 0) {
                return this.matches(value.slice(separator + 1))
                    && Boolean(this.parent?.closest(value.slice(0, separator)));
            }
            return value.startsWith('.') ? this.classes.has(value.slice(1)) : this.tag === value;
        });
    }
    closest(selector) {
        return this.matches(selector) ? this : this.parent?.closest(selector) ?? null;
    }
    querySelectorAll(selector) {
        return this.children.flatMap(child => [
            ...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)
        ]);
    }
    querySelector(selector) { return this.querySelectorAll(selector)[0]; }
    contains(target) { return this === target || this.children.some(child => child.contains(target)); }
    getBoundingClientRect() { return { top: this.top, left: 0, width: 400, height: 60 }; }
    setAttribute(name, value) { this.attributes[name] = value; }
    showPopover() { this.open = true; }
    hidePopover() { this.open = false; }
    focus() { document.activeElement = this; }
    setPointerCapture(id) { this.captured.add(id); }
    hasPointerCapture(id) { return this.captured.has(id); }
    releasePointerCapture(id) { this.captured.delete(id); }
    addEventListener(name, listener) { (this.listeners[name] ??= []).push(listener); }
    async emit(name, event) {
        for (const listener of this.listeners[name] ?? []) await listener(event);
    }
}

function fixture() {
    globalThis.document = new Element();
    globalThis.window = new Element();
    globalThis.innerWidth = 800;
    globalThis.innerHeight = 600;
    const list = new Element();
    const rows = [0, 68, 136].map(top => {
        const row = new Element('cc-row-shell', list, top);
        row.dataset.canRemove = 'true';
        new Element('row-swipe-delete', row, top, 'button');
        const card = new Element('cc-row', row, top);
        new Element('row-drag-handle', card, top, 'button');
        new Element('currency-code', card, top, 'button');
        const amountArea = new Element('amount-area', card, top);
        new Element('amount-input', amountArea, top, 'input');
        const menu = new Element('row-menu', row, top);
        new Element('', menu, top, 'button');
        return row;
    });
    const calls = [];
    initialize(list, { invokeMethodAsync: async (...args) => calls.push(args) });
    const send = (name, target, values = {}) => {
        if (name === 'lostpointercapture') target.releasePointerCapture(values.pointerId ?? 1);
        return list.emit(name, {
            target, pointerId: 1, isPrimary: true, button: 0, pointerType: 'mouse',
            clientX: 100, clientY: 0, preventDefault() {}, stopImmediatePropagation() {}, ...values
        });
    };
    return { list, rows, calls, send, cleanup: () => dispose(list) };
}

for (const cancel of ['pointercancel', 'lostpointercapture']) {
    test(`${cancel} 不提交拖动，下一次拖动仍可提交`, async () => {
        const f = fixture();
        const handle = f.rows[0].querySelector('.row-drag-handle');
        await f.send('pointerdown', handle);
        await f.send('pointermove', handle, { clientY: 136 });
        await f.send(cancel, handle);
        await f.send('pointerup', handle, { clientY: 136 });
        assert.deepEqual(f.calls, []);
        assert.equal(f.rows[0].style.transform, '');
        await f.send('pointerdown', handle);
        await f.send('pointermove', handle, { clientY: 136 });
        await f.send('pointerup', handle, { clientY: 136 });
        assert.deepEqual(f.calls, [['ReorderAsync', 0, 2]]);
        f.cleanup();
    });
}

test('其他指针不能移动或提交当前拖动', async () => {
    const f = fixture();
    const handle = f.rows[0].querySelector('.row-drag-handle');
    await f.send('pointerdown', handle);
    await f.send('pointermove', handle, { pointerId: 2, clientY: 136 });
    await f.send('pointerup', handle, { pointerId: 2 });
    assert.deepEqual(f.calls, []);
    await f.send('pointermove', handle, { clientY: 68 });
    await f.send('pointerup', handle);
    assert.deepEqual(f.calls, [['ReorderAsync', 0, 1]]);
    f.cleanup();
});

test('右键和非主触点不能开始拖动', async () => {
    const f = fixture();
    const handle = f.rows[0].querySelector('.row-drag-handle');
    for (const values of [{ button: 2 }, { isPrimary: false }]) {
        await f.send('pointerdown', handle, values);
        await f.send('pointermove', handle, { clientY: 136 });
        await f.send('pointerup', handle);
    }
    assert.deepEqual(f.calls, []);
    f.cleanup();
});

test('触控左滑只展开删除，且只允许一行展开', async () => {
    const f = fixture();
    for (const row of f.rows.slice(0, 2)) {
        const card = row.querySelector('.cc-row');
        await f.send('pointerdown', card, { pointerType: 'touch' });
        await f.send('pointermove', card, { clientX: 30 });
        await f.send('pointerup', card, { clientX: 30 });
    }
    assert.equal(f.rows[0].classList.contains('is-revealed'), false);
    assert.equal(f.rows[1].classList.contains('is-revealed'), true);
    assert.equal(f.rows[1].querySelector('.row-swipe-delete').tabIndex, 0);
    assert.deepEqual(f.calls, []);
    f.cleanup();
});

test('纵向滚动、输入框选区、鼠标横拖不会展开删除', async () => {
    const f = fixture();
    const card = f.rows[0].querySelector('.cc-row');
    await f.send('pointerdown', card, { pointerType: 'touch' });
    await f.send('pointermove', card, { clientY: 40 });
    await f.send('pointermove', card, { clientX: 0 });
    await f.send('pointerup', card, { clientX: 0 });
    for (const [target, pointerType] of [[card, 'mouse'], [f.rows[0].querySelector('input'), 'touch']]) {
        await f.send('pointerdown', target, { pointerType });
        await f.send('pointermove', target, { clientX: 0 });
        await f.send('pointerup', target, { clientX: 0 });
    }
    assert.equal(f.rows[0].classList.contains('is-revealed'), false);
    f.cleanup();
});

test('取消滑动不展开删除，最低行数限制不被手势绕过', async () => {
    const f = fixture();
    const card = f.rows[0].querySelector('.cc-row');
    await f.send('pointerdown', card, { pointerType: 'touch' });
    await f.send('pointermove', card, { clientX: 0 });
    await f.send('pointercancel', card);
    assert.equal(f.rows[0].classList.contains('is-revealed'), false);
    f.rows[0].dataset.canRemove = 'false';
    await f.send('pointerdown', card, { pointerType: 'touch' });
    await f.send('pointermove', card, { clientX: 0 });
    await f.send('pointerup', card, { clientX: 0 });
    assert.equal(f.rows[0].classList.contains('is-revealed'), false);
    assert.deepEqual(f.calls, []);
    f.cleanup();
});

test('子元素隐式捕获转交整行不会把已露出的删除按钮复位', async () => {
    const f = fixture();
    const row = f.rows[0];
    const child = row.querySelector('.currency-code');
    child.setPointerCapture(1);
    await f.send('pointerdown', child, { pointerType: 'touch' });
    await f.send('pointermove', child, { clientX: 70 });
    assert.equal(row.querySelector('.cc-row').style.transform, 'translateX(-30px)');
    assert.equal(row.hasPointerCapture(1), true);
    await f.send('lostpointercapture', child);
    assert.equal(row.querySelector('.cc-row').style.transform, 'translateX(-30px)');
    await f.send('pointermove', row, { clientX: 20 });
    await f.send('pointerup', row, { clientX: 20 });
    assert.equal(row.classList.contains('is-revealed'), true);
    f.cleanup();
});

test('行中间空白支持滑动与右键，金额文字保留原生菜单', async () => {
    const f = fixture();
    const row = f.rows[0];
    const area = row.querySelector('.amount-area');
    await f.send('pointerdown', area, { pointerType: 'touch' });
    await f.send('pointermove', area, { clientX: 20 });
    await f.send('pointerup', area, { clientX: 20 });
    assert.equal(row.classList.contains('is-revealed'), true);
    await f.send('contextmenu', row.querySelector('input'));
    assert.equal(row.querySelector('.row-menu').open, false);
    await f.send('contextmenu', area);
    assert.equal(row.querySelector('.row-menu').open, true);
    f.cleanup();
});

test('触控长按中间空白打开行菜单，不依赖浏览器 contextmenu', async t => {
    t.mock.timers.enable({ apis: ['setTimeout'] });
    const f = fixture();
    const row = f.rows[0];
    const area = row.querySelector('.amount-area');
    await f.send('pointerdown', area, { pointerType: 'touch' });
    t.mock.timers.tick(550);
    assert.equal(row.querySelector('.row-menu').open, true);
    assert.deepEqual(f.calls, []);
    f.cleanup();
});

test('滑动、抬手或系统取消都撤销待触发的长按菜单', async t => {
    t.mock.timers.enable({ apis: ['setTimeout'] });
    const f = fixture();
    const row = f.rows[0];
    const area = row.querySelector('.amount-area');
    for (const action of ['pointermove', 'pointerup', 'pointercancel']) {
        await f.send('pointerdown', area, { pointerType: 'touch' });
        await f.send(action, area, { clientX: 50 });
        t.mock.timers.tick(600);
        assert.equal(row.querySelector('.row-menu').open, false);
        await f.send('pointercancel', area);
    }
    f.cleanup();
});

test('长按松手及补发点击不关闭菜单，下一次点击菜单仍可执行', async t => {
    t.mock.timers.enable({ apis: ['setTimeout'] });
    const f = fixture();
    const row = f.rows[0];
    const area = row.querySelector('.amount-area');
    const menu = row.querySelector('.row-menu');
    const button = menu.querySelector('button');
    await f.send('pointerdown', area, { pointerType: 'touch' });
    t.mock.timers.tick(550);
    assert.equal(menu.open, true);
    assert.notEqual(document.activeElement, button);
    await f.send('pointerup', row);
    let swallowed = false;
    await f.send('click', button, { stopImmediatePropagation: () => swallowed = true });
    assert.equal(swallowed, true);
    assert.equal(menu.open, true);
    await f.send('pointerdown', button, { pointerType: 'touch' });
    await f.send('click', button);
    assert.equal(menu.open, false);
    f.cleanup();
});

test('手动菜单松手后保留，后续外部按下才关闭', async () => {
    const f = fixture();
    const area = f.rows[0].querySelector('.amount-area');
    await f.send('contextmenu', area);
    const menu = f.rows[0].querySelector('.row-menu');
    assert.equal(menu.open, true);
    await f.send('pointerup', area);
    assert.equal(menu.open, true);
    await document.emit('pointerdown', { isPrimary: true, target: f.rows[1] });
    assert.equal(menu.open, false);
    f.cleanup();
});
