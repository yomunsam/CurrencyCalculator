const lists = new WeakMap();

export function initialize(list, reference) {
    const controller = new AbortController();
    const options = { signal: controller.signal };
    let gesture = null;
    let revealed = null;
    let suppressedClick = null;
    let committing = false;
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
        for (const row of current.rows) {
            row.style.transform = '';
            row.classList.remove('is-dragging', 'is-swiping');
        }
        current.row.querySelector('.cc-row').style.transform = '';
        list.classList.remove('is-gesturing');
        if (current.capture.hasPointerCapture(current.id)) current.capture.releasePointerCapture(current.id);
    }
    function menuFor(row) { return row.querySelector('.row-menu'); }
    function openMenu(row, x, y) {
        closeRevealed();
        const menu = menuFor(row);
        menu.showPopover();
        const rect = menu.getBoundingClientRect();
        menu.style.left = `${Math.max(8, Math.min(x, innerWidth - rect.width - 8))}px`;
        menu.style.top = `${Math.max(8, Math.min(y, innerHeight - rect.height - 8))}px`;
        menu.querySelector('button:not(:disabled)')?.focus();
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
        }
    }, options);

    list.addEventListener('pointermove', event => {
        const g = gesture;
        if (!g || event.pointerId !== g.id) return;
        const dx = event.clientX - g.x;
        const dy = event.clientY - g.y;
        if (!g.active) {
            if (g.kind === 'swipe' && Math.abs(dy) > 10 && Math.abs(dy) >= Math.abs(dx)) {
                reset();
                return;
            }
            if (g.kind === 'drag' ? Math.abs(dy) < 6 : Math.abs(dx) < 12 || Math.abs(dx) <= Math.abs(dy)) return;
            g.active = true;
            g.capture.setPointerCapture(g.id);
            list.classList.add('is-gesturing');
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
        if (g.kind === 'swipe' && g.active && g.row.dataset.canRemove === 'true') {
            const dx = event.clientX - g.x;
            reveal(g.row, g.wasOpen ? dx < 40 : dx < -40);
        }
        reset();
        if (g.kind === 'drag' && g.active && g.source !== g.target) {
            committing = true;
            try {
                await reference.invokeMethodAsync('ReorderAsync', g.source, g.target);
            } finally {
                committing = false;
            }
        }
    }, options);
    for (const name of ['pointercancel', 'lostpointercapture']) {
        list.addEventListener(name, event => {
            if (gesture?.id === event.pointerId) {
                if (gesture.active) suppressedClick = gesture.row;
                reset();
            }
        }, options);
    }
    window.addEventListener('blur', reset, options);

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
        if (event.target.closest('.row-menu button')) {
            menuFor(row).hidePopover();
            row.querySelector('.row-drag-handle').focus({ preventScroll: true });
        }
    }, { ...options, capture: true });

    list.addEventListener('contextmenu', event => {
        if (event.target.closest('input')) return;
        const row = event.target.closest('.cc-row-shell');
        if (!row) return;
        event.preventDefault();
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
            menu.hidePopover();
            row.querySelector('.row-drag-handle').focus();
        }
    }, options);
    document.addEventListener('pointerdown', event => {
        if (revealed && !revealed.contains(event.target)) closeRevealed();
    }, options);
    lists.set(list, { controller, reset });
}

export function dispose(list) {
    lists.get(list)?.reset();
    lists.get(list)?.controller.abort();
    lists.delete(list);
}

export function focusRow(list, index) {
    list.querySelectorAll('.row-drag-handle')[index]?.focus({ preventScroll: true });
}
