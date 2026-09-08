// 浏览器禁用存储或容量已满时，保留当前页面功能，并明确标记无法持久保存。
window.ccStorage = {
    available: true,
    get(key) {
        try { return localStorage.getItem(key); }
        catch { this.unavailable(); return null; }
    },
    set(key, value) {
        try { localStorage.setItem(key, value); return true; }
        catch { this.unavailable(); return false; }
    },
    remove(key) {
        try { localStorage.removeItem(key); }
        catch { this.unavailable(); }
    },
    unavailable() {
        if (!this.available) return;
        this.available = false;
        window.dispatchEvent(new Event('cc-storage-unavailable'));
    }
};
