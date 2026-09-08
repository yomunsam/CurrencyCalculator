self.importScripts('./service-worker-assets.js');

// 入口和资源使用同一份发布快照；更新等待旧页面全部关闭，避免混用两版程序集。
const baseUrl = new URL('./', self.location.href);
const indexUrl = new URL('index.html', baseUrl).href;
const cacheNamePrefix = `cc-offline-${encodeURIComponent(baseUrl.pathname)}-`;
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssets = self.assetsManifest.assets.filter(asset =>
    /\.(dll|wasm|html|js|json|css|woff2?|png|jpe?g|gif|ico|blat|dat|webmanifest|svg)$/.test(asset.url)
    && !/^service-worker(?:\.published|-assets)?\.js$/.test(asset.url));
const assetUrls = new Set(offlineAssets.map(asset => new URL(asset.url, baseUrl).href));

self.addEventListener('install', event => event.waitUntil(onInstall()));
self.addEventListener('activate', event => event.waitUntil(onActivate()));
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    if (event.request.method !== 'GET' || url.origin !== baseUrl.origin || !url.pathname.startsWith(baseUrl.pathname)) return;
    event.respondWith(onFetch(event.request));
});
self.addEventListener('message', event => {
    if (event.data?.type === 'GET_OFFLINE_STATUS') {
        event.ports[0]?.postMessage({ offlineReady: true, version: self.assetsManifest.version });
    }
});

async function onInstall() {
    const requests = offlineAssets.map(asset => new Request(new URL(asset.url, baseUrl), {
        integrity: asset.hash, cache: 'no-cache'
    }));
    const cache = await caches.open(cacheName);
    await cache.addAll(requests);
}

async function onActivate() {
    const keys = await caches.keys();
    for (const key of keys) {
        if (key.startsWith(cacheNamePrefix) && key !== cacheName) {
            await caches.delete(key);
        } else if (key.startsWith('offline-cache-')) {
            // 清理之前无作用域的缓存时，先确认它属于当前应用，不能删除同域其他 PWA。
            const legacy = await caches.open(key);
            if (await legacy.match(indexUrl)) await caches.delete(key);
        }
    }
}

async function onFetch(request) {
    const cache = await caches.open(cacheName);
    if (request.mode === 'navigate') {
        return (await cache.match(indexUrl)) || fetch(request);
    }
    const url = new URL(request.url);
    url.search = '';
    if (assetUrls.has(url.href)) {
        const cached = await cache.match(url.href);
        if (cached) return cached;
    }
    return fetch(request);
}
