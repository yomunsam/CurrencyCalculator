export async function copyValue(value) {
    if (navigator.clipboard?.writeText) {
        try {
            await navigator.clipboard.writeText(value);
            return true;
        } catch {
            // 被浏览器拒绝时仍可尝试当前页面的复制动作。
        }
    }

    // 局域网 HTTP 没有 Clipboard API，保留这条真实调试环境需要的路径。
    const previousFocus = document.activeElement;
    const field = document.createElement('textarea');
    field.value = value;
    field.readOnly = true;
    field.style.cssText = 'position:fixed;left:0;top:0;opacity:0;pointer-events:none;';
    document.body.appendChild(field);
    try {
        field.select();
        field.setSelectionRange(0, value.length);
        return document.execCommand('copy');
    } catch {
        return false;
    } finally {
        field.remove();
        if (previousFocus?.isConnected) previousFocus.focus({ preventScroll: true });
    }
}
