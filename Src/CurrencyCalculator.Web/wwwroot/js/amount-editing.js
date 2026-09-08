// 只修改选区；不重排千位分隔符，避免光标在输入中跳动。
export function editSelection(value, start, end, key) {
    if (key === 'clear') return { value: '', caret: 0 };
    if (key === 'backspace') {
        if (start === end) start = Math.max(0, start - 1);
        key = '';
    }
    return {
        value: value.slice(0, start) + key + value.slice(end),
        caret: start + key.length
    };
}
