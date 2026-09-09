import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

const code = await readFile(new URL('../../Src/CurrencyCalculator.Web/wwwroot/service-worker.published.js', import.meta.url), 'utf8');

function setup(path = '/', existing = new Map(), failInstall = false) {
    const base = `https://example.test${path}`;
    const events = new Map();
    const requested = [];
    const network = [];
    const buckets = existing;
    function bucket(name) {
        if (!buckets.has(name)) buckets.set(name, new Map());
        const entries = buckets.get(name);
        return {
            async match(key) { return entries.get(typeof key === 'string' ? key : key.url); },
            async addAll(requests) {
                requested.push(...requests);
                if (failInstall) throw new Error('integrity mismatch');
                for (const request of requests) entries.set(request.url, `cached:${request.url}`);
            }
        };
    }
    const context = {
        URL, Request, Response,
        self: {
            location: { href: `${base}service-worker.js` },
            assetsManifest: { version: 'new', assets: ['index.html', 'css/app.css', 'icons.woff2', 'debug.pdb', 'service-worker.js'].map(url => ({ url, hash: 'sha256-test' })) },
            importScripts() {},
            addEventListener(type, fn) { events.set(type, fn); },
            skipWaiting() { assert.fail('更新不得强制接管旧页面'); },
            clients: { claim() { assert.fail('更新不得强制接管旧页面'); } }
        },
        caches: { open: async name => bucket(name), keys: async () => [...buckets.keys()], delete: async name => buckets.delete(name) },
        fetch: async request => { network.push(request.url); return 'network'; }
    };
    vm.runInNewContext(code, context);
    return {
        requested, network, buckets,
        async lifecycle(type) {
            let work;
            events.get(type)({ waitUntil(promise) { work = promise; } });
            await work;
        },
        async get(relative, mode = 'navigate', method = 'GET') {
            let response;
            events.get('fetch')({ request: { url: new URL(relative, base).href, method, mode }, respondWith(value) { response = value; } });
            return response;
        }
    };
}

for (const path of ['/', '/CurrencyCalculator/']) {
    test(`${path} 离线导航始终使用同版入口，字体包含在安装缓存`, async () => {
        const f = setup(path);
        await f.lifecycle('install');
        assert.equal(f.requested.length, 3);
        assert.ok(f.requested.every(request => request.url.startsWith(`https://example.test${path}`) && request.cache === 'no-cache'));
        assert.equal(await f.get('?from=installed'), `cached:https://example.test${path}index.html`);
        assert.equal(await f.get('index.html?x=1'), `cached:https://example.test${path}index.html`);
        assert.equal(await f.get('icons.woff2?v=123', 'cors'), `cached:https://example.test${path}icons.woff2`);
        assert.deepEqual(f.network, []);
    });
}

for (const path of ['/', '/CurrencyCalculator/']) {
    test(`${path} 已缓存的重定向入口可用于刷新和离线导航`, async () => {
        const f = setup(path);
        await f.lifecycle('install');
        const entries = f.buckets.get(`cc-offline-${encodeURIComponent(path)}-new`);
        const html = '<!doctype html><title>cached release</title>';
        // 模拟 Cache API 返回跟随 index.html 重定向后保存的响应。
        for (const target of ['', '?from=installed', 'index.html']) {
            const redirected = new Response(html, {
                headers: { 'Content-Type': 'text/html; charset=utf-8' }
            });
            Object.defineProperty(redirected, 'redirected', { value: true });
            entries.set(`https://example.test${path}index.html`, redirected);
            const response = await f.get(target);
            assert.equal(response.redirected, false);
            assert.equal(response.status, 200);
            assert.equal(response.headers.get('Content-Type'), 'text/html; charset=utf-8');
            assert.equal(await response.text(), html);
        }
        assert.deepEqual(f.network, []);
    });
}

test('外部 API 和写请求不进入应用缓存', async () => {
    const f = setup('/CurrencyCalculator/');
    await f.lifecycle('install');
    assert.equal(await f.get('https://api.example.test/latest', 'cors'), undefined);
    assert.equal(await f.get('/other/index.html', 'navigate'), undefined);
    assert.equal(await f.get('save', 'cors', 'POST'), undefined);
    assert.equal(await f.get('uncached.json', 'cors'), 'network');
});

test('新快照安装失败不会删除旧缓存', async () => {
    const old = new Map([['cc-offline-%2FCurrencyCalculator%2F-old', new Map()]]);
    const f = setup('/CurrencyCalculator/', old, true);
    await assert.rejects(f.lifecycle('install'), /integrity mismatch/);
    assert.ok(f.buckets.has('cc-offline-%2FCurrencyCalculator%2F-old'));
});

test('激活只清理本应用旧缓存，保留同域其他应用', async () => {
    const base = 'https://example.test/CurrencyCalculator/';
    const old = new Map([
        ['cc-offline-%2FCurrencyCalculator%2F-old', new Map()],
        ['cc-offline-%2FOther%2F-old', new Map()],
        ['offline-cache-legacy', new Map([[`${base}index.html`, 'old']])],
        ['offline-cache-other', new Map([['https://example.test/Other/index.html', 'other']])]
    ]);
    const f = setup('/CurrencyCalculator/', old);
    await f.lifecycle('install');
    await f.lifecycle('activate');
    assert.ok(!old.has('cc-offline-%2FCurrencyCalculator%2F-old'));
    assert.ok(!old.has('offline-cache-legacy'));
    assert.ok(old.has('cc-offline-%2FOther%2F-old'));
    assert.ok(old.has('offline-cache-other'));
});
