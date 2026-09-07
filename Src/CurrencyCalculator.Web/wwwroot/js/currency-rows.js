import { copyValue } from './clipboard.js';

const lists = new WeakMap();

export function initialize(list, reference) {
    const controller = new AbortController();
    const options = { signal: controller.signal };
    let gesture = null;
    let revealed = null;
    let suppressedClick = null;
    let committing = false;
    let activeMenu = null;
    const rows = () => [...list.querySelectorAll('.cc-row-shell')];

    function reveal(row, open) {
        row.classList.toggle('is-revealed', open);
        const button = row.querySelector('.row-swipe-delete');
        button.tabIndex = open ? 0 : -1;
        button.setAttribute('aria-hidden', String(!open));
        revealed = open ? row : null;
    }
    function closeRevealed() {
        if (revealed) reveal(revealed, false);
    }
    function reset() {
        if (!gesture) return;
        const current = gesture;
        gesture = null;
        clearTimeout(current.longPressTimer);
        for (const row of current.rows) {
            row.style.transform = '';
            row.classList.remove('is-dragging', 'is-swiping');
        }
        current.row.querySelector('.cc-row').style.transform = '';
        if (current.capture.hasPointerCapture(current.id)) current.capture.releasePointerCapture(current.id);
    }
    function menuFor(row) { return row.querySelector('.row-menu'); }
    function closeMenu() {
        if (!activeMenu) return;
        activeMenu.hidePopover();
        activeMenu = null;
    }
    function openMenu(row, x, y, focus = true) {
        closeRevealed();
        closeMenu();
        const menu = menuFor(row);
        menu.showPopover();
        activeMenu = menu;
        const rect = menu.getBoundingClientRect();
        menu.style.left = `${Math.max(8, Math.min(x, innerWidth - rect.width - 8))}px`;
        menu.style.top = `${Math.max(8, Math.min(y, innerHeight - rect.height - 8))}px`;
        if (focus) menu.querySelector('button:not(:disabled)')?.focus();
    }
    function openHeldMenu(pending) {
        clearTimeout(pending.longPressTimer);
        pending.kind = 'menu';
        pending.active = true;
        pending.capture.setPointerCapture(pending.id);
        // 保留触发长按的指针，松手前既不聚焦菜单，也不让它点击菜单项。
        openMenu(pending.row, pending.x, pending.y, false);
    }

    list.addEventListener('pointerdown', event => {
        if (!event.isPrimary || event.button !== 0 || gesture || committing) return;
        // 下一次独立点击不应被上一次手势的点击抑制状态吞掉。
        suppressedClick = null;
        const row = event.target.closest('.cc-row-shell');
        if (!row || event.target.closest('.row-menu, .row-swipe-delete')) return;
        const handle = event.target.closest('.row-drag-handle');
        if (event.target.closest('.row-clear')) {
            if (document.activeElement === row.querySelector('input')) event.preventDefault();
            return;
        }
        if (!handle && (event.pointerType !== 'touch' || event.target.closest('input, .row-btn'))) return;
        const wasOpen = row === revealed;
        if (revealed && revealed !== row) closeRevealed();
        const currentRows = rows();
        const source = currentRows.indexOf(row);
        const capture = handle || row;
        gesture = {
            id: event.pointerId, row, rows: currentRows, source, target: source,
            x: event.clientX, y: event.clientY, kind: handle ? 'drag' : 'swipe',
            capture, active: false, wasOpen,
            tops: currentRows.map(item => item.getBoundingClientRect().top)
        };
        if (handle) {
            event.preventDefault();
            closeRevealed();
            capture.setPointerCapture(event.pointerId);
        } else {
            const pending = gesture;
            pending.longPressTimer = setTimeout(() => {
                if (gesture !== pending) return;
                openHeldMenu(pending);
            }, 550);
        }
    }, options);

    list.addEventListener('pointermove', event => {
        const g = gesture;
        if (!g || event.pointerId !== g.id) return;
        if (g.kind === 'menu') return;
        const dx = event.clientX - g.x;
        const dy = event.clientY - g.y;
        if (Math.hypot(dx, dy) > 8) clearTimeout(g.longPressTimer);
        if (!g.active) {
            if (g.kind === 'swipe' && Math.abs(dy) > 10 && Math.abs(dy) >= Math.abs(dx)) {
                reset();
                return;
            }
            if (g.kind === 'drag' ? Math.abs(dy) < 6 : Math.abs(dx) < 12 || Math.abs(dx) <= Math.abs(dy)) return;
            g.active = true;
            g.capture.setPointerCapture(g.id);
            g.row.classList.add(g.kind === 'drag' ? 'is-dragging' : 'is-swiping');
        }
        if (g.kind === 'swipe') {
            if (g.row.dataset.canRemove !== 'true') return;
            const offset = Math.max(-88, Math.min(0, dx - (g.wasOpen ? 88 : 0)));
            g.row.querySelector('.cc-row').style.transform = `translateX(${offset}px)`;
            return;
        }
        const offset = Math.max(g.tops[0] - g.tops[g.source], Math.min(dy, g.tops.at(-1) - g.tops[g.source]));
        g.target = g.tops.reduce((best, top, index) =>
            Math.abs(top - g.tops[g.source] - offset) < Math.abs(g.tops[best] - g.tops[g.source] - offset) ? index : best, g.source);
        g.rows.forEach((row, index) => {
            let shift = 0;
            if (index === g.source) shift = offset;
            else if (index > g.source && index <= g.target) shift = g.tops[index - 1] - g.tops[index];
            else if (index < g.source && index >= g.target) shift = g.tops[index + 1] - g.tops[index];
            row.style.transform = `translateY(${shift}px)`;
        });
    }, options);

    list.addEventListener('pointerup', async event => {
        const g = gesture;
        if (!g || event.pointerId !== g.id) return;
        if (g.active) suppressedClick = g.row;
        if (g.kind === 'menu') event.preventDefault();
        if (g.kind === 'swipe' && g.active && g.row.dataset.canRemove === 'true') {
            const dx = event.clientX - g.x;
            reveal(g.row, g.wasOpen ? dx < 40 : dx < -40);
        }
        reset();
        if (g.kind === 'menu') activeMenu?.querySelector('button:not(:disabled)')?.focus();
        if (g.kind === 'drag' && g.active && g.source !== g.target) {
            committing = true;
            try {
                await reference.invokeMethodAsync('ReorderAsync', g.source, g.target);
            } finally {
                committing = false;
            }
        }
    }, options);
    list.addEventListener('pointercancel', event => {
        if (gesture?.id !== event.pointerId) return;
        if (gesture.active) suppressedClick = gesture.row;
        reset();
    }, options);
    list.addEventListener('lostpointercapture', event => {
        const g = gesture;
        if (!g || event.pointerId !== g.id) return;
        // 子元素的隐式捕获转交给整行时也会冒泡此事件，不能据此取消滑动。
        if (event.target !== g.capture || g.capture.hasPointerCapture(g.id)) return;
        if (g.active) suppressedClick = g.row;
        reset();
    }, options);
    window.addEventListener('blur', () => {
        reset();
        closeMenu();
    }, options);

    list.addEventListener('click', event => {
        const row = event.target.closest('.cc-row-shell');
        if (row && row === suppressedClick) {
            suppressedClick = null;
            event.preventDefault();
            event.stopImmediatePropagation();
            return;
        }
        const handle = event.target.closest('.row-drag-handle');
        if (handle) {
            const bounds = handle.getBoundingClientRect();
            openMenu(row, bounds.left, bounds.bottom + 4);
        }
        const menuButton = event.target.closest('.row-menu button');
        if (menuButton && !menuButton.disabled) {
            if (menuButton.classList.contains('row-copy')) {
                // 在用户点击事件内开始复制，避免跨越 .NET 异步调用后丢失剪贴板权限。
                copyValue(menuButton.dataset.copyValue).then(success => {
                    window.ccToast.show(success ? menuButton.dataset.copySuccess : menuButton.dataset.copyFailed, 2000);
                });
            }
            closeMenu();
            row.querySelector('.row-drag-handle').focus({ preventScroll: true });
        }
    }, { ...options, capture: true });

    list.addEventListener('contextmenu', event => {
        if (event.target.closest('input')) return;
        const row = event.target.closest('.cc-row-shell');
        if (!row) return;
        event.preventDefault();
        if (event.target.closest('.row-menu') || gesture?.kind === 'menu') return;
        suppressedClick = row;
        if (gesture?.kind === 'swipe' && !gesture.active) {
            openHeldMenu(gesture);
            return;
        }
        if (gesture?.active) return;
        reset();
        openMenu(row, event.clientX, event.clientY);
    }, options);
    list.addEventListener('keydown', event => {
        const row = event.target.closest('.cc-row-shell');
        if (!row) return;
        if ((event.key === 'ContextMenu' || (event.shiftKey && event.key === 'F10')) && !event.target.closest('input')) {
            event.preventDefault();
            const bounds = row.getBoundingClientRect();
            openMenu(row, bounds.left, bounds.bottom);
        }
        if (event.key === 'Escape') {
            reset();
            closeRevealed();
            if (activeMenu) {
                const menuRow = activeMenu.closest('.cc-row-shell');
                closeMenu();
                menuRow.querySelector('.row-drag-handle').focus();
            }
        }
        const menu = event.target.closest('.row-menu');
        if (!menu) return;
        const buttons = [...menu.querySelectorAll('button:not(:disabled)')];
        const index = buttons.indexOf(document.activeElement);
        if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
            event.preventDefault();
            const next = event.key === 'Home' ? 0 : event.key === 'End' ? buttons.length - 1
                : (index + (event.key === 'ArrowDown' ? 1 : -1) + buttons.length) % buttons.length;
            buttons[next]?.focus();
        }
        if (event.key === 'Escape' || event.key === 'Tab') {
            closeMenu();
            row.querySelector('.row-drag-handle').focus();
        }
    }, options);
    document.addEventListener('pointerdown', event => {
        if (!event.isPrimary) return;
        if (activeMenu && !activeMenu.contains(event.target)) closeMenu();
        if (revealed && !revealed.contains(event.target)) closeRevealed();
    }, { ...options, capture: true });
    lists.set(list, { controller, reset, closeMenu });
}

export function dispose(list) {
    lists.get(list)?.reset();
    lists.get(list)?.closeMenu();
    lists.get(list)?.controller.abort();
    lists.delete(list);
}

export function focusRow(list, index) {
    list.querySelectorAll('.row-drag-handle')[index]?.focus({ preventScroll: true });
}
