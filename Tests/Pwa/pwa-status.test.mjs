import test from 'node:test';
import assert from 'node:assert/strict';

let scenario = 0;
async function setup(t, { secure = true, waiting = false, development = false, fail = false } = {}) {
    const changes = [];
    let calls = 0;
    const registration = new EventTarget();
    registration.active = {
        state: 'activated',
        postMessage(message, ports) {
            assert.equal(message.type, 'GET_OFFLINE_STATUS');
            ports[0].postMessage({ offlineReady: !development, development });
        }
    };
    registration.waiting = waiting ? { postMessage() { assert.fail('不能强制跳过等待阶段'); } } : null;
    const browser = new EventTarget();
    browser.isSecureContext = secure;
    browser.ccStorage = { available: true };
    globalThis.window = browser;
    globalThis.document = Object.assign(new EventTarget(), { baseURI: 'https://example.test/CurrencyCalculator/', visibilityState: 'visible' });
    Object.defineProperty(globalThis, 'navigator', { configurable: true, value: {
        serviceWorker: {
            async register(url, options) {
                calls++;
                assert.equal(url.href, 'https://example.test/CurrencyCalculator/service-worker.js');
                assert.equal(options.updateViaCache, 'none');
                if (fail) throw new Error('offline');
                return registration;
            }
        }
    } });
    const module = await import(`../../Src/CurrencyCalculator.Web/wwwroot/js/pwa.js?scenario=${++scenario}`);
    const subscription = module.subscribe({
        async invokeMethodAsync(method, status, canPersist) { changes.push({ status, canPersist }); }
    });
    t.after(() => module.unsubscribe(subscription));
    return {
        changes, calls: () => calls,
        async waitFor(status) {
            const deadline = Date.now() + 2000;
            while (!changes.some(change => change.status === status)) {
                if (Date.now() > deadline) assert.fail(`Missing status ${status}`);
                await new Promise(resolve => setTimeout(resolve, 5));
            }
        }
    };
}

test('不安全的局域网 HTTP 不尝试注册 Service Worker', async t => {
    const f = await setup(t, { secure: false });
    await f.waitFor('PwaUnsupported');
    assert.equal(f.calls(), 0);
});

test('发布 worker 确认后才显示离线就绪，并能通知存储不可用', async t => {
    const f = await setup(t);
    await f.waitFor('PwaReady');
    window.ccStorage.available = false;
    window.dispatchEvent(new Event('cc-storage-unavailable'));
    assert.equal(f.changes.at(-1).canPersist, false);
});

test('开发 worker 不会被误报为离线就绪', async t => {
    const f = await setup(t, { development: true });
    await f.waitFor('PwaDevelopment');
    assert.ok(!f.changes.some(change => change.status === 'PwaReady'));
});

test('等待中的更新仅给出状态，不接管或重载当前页面', async t => {
    const f = await setup(t, { waiting: true });
    await f.waitFor('PwaUpdateReady');
});

test('注册失败显示真实状态，不产生未处理的拒绝', async t => {
    const warning = t.mock.method(console, 'warn', () => {});
    const f = await setup(t, { fail: true });
    await f.waitFor('PwaFailed');
    assert.equal(warning.mock.callCount(), 1);
});
