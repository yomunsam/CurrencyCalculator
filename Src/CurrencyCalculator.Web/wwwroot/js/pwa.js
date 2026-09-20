const subscribers = new Map();
let nextId = 0;
let status = 'PwaPreparing';
let registration;
let started = false;
let lastCheck = 0;
let clearing = false;

function notify() {
    for (const reference of subscribers.values()) {
        reference.invokeMethodAsync('UpdateStatus', status, window.ccStorage.available)
            .catch(error => console.debug('PWA status listener disconnected', error));
    }
}

async function inspect() {
    if (registration.waiting) {
        status = 'PwaUpdateReady';
    } else if (registration.active) {
        // 等待激活中的 worker 完成缓存后再声明离线就绪。
        if (registration.active.state !== 'activated') return;
        const active = registration.active;
        const result = await new Promise(resolve => {
            const channel = new MessageChannel();
            const finish = value => {
                clearTimeout(timeout);
                channel.port1.close();
                channel.port2.close();
                resolve(value);
            };
            const timeout = setTimeout(() => finish(null), 3000);
            channel.port1.onmessage = event => finish(event.data);
            active.postMessage({ type: 'GET_OFFLINE_STATUS' }, [channel.port2]);
        });
        if (registration.waiting) status = 'PwaUpdateReady';
        else status = result?.development ? 'PwaDevelopment' : result?.offlineReady ? 'PwaReady' : 'PwaUnknown';
    } else {
        status = 'PwaPreparing';
    }
    notify();
}

async function register() {
    try {
        registration = await navigator.serviceWorker.register(new URL('service-worker.js', document.baseURI), {
            updateViaCache: 'none'
        });
        const trackInstalling = () => {
            const worker = registration.installing;
            worker?.addEventListener('statechange', () => {
                if (worker.state === 'redundant' && !registration.active) {
                    status = 'PwaFailed';
                    notify();
                } else {
                    inspect().catch(reportFailure);
                }
            });
        };
        registration.addEventListener('updatefound', trackInstalling);
        trackInstalling();
        if (registration.active?.state === 'activating') {
            registration.active.addEventListener('statechange', () => inspect().catch(reportFailure), { once: true });
        }
        await inspect();
    } catch (error) {
        reportFailure(error);
    }
}

function reportFailure(error) {
    console.warn('PWA cache/update check failed', error);
    if (!registration?.active) status = 'PwaFailed';
    notify();
}

function checkForUpdate() {
    if (clearing || document.visibilityState === 'hidden' || Date.now() - lastCheck < 60000) return;
    lastCheck = Date.now();
    if (!registration) register();
    else registration.update().then(inspect).catch(reportFailure);
}

export function subscribe(reference) {
    const id = ++nextId;
    subscribers.set(id, reference);
    if (!started) {
        started = true;
        window.addEventListener('cc-storage-unavailable', notify);
        if (!window.isSecureContext || !('serviceWorker' in navigator)) {
            status = 'PwaUnsupported';
        } else {
            lastCheck = Date.now();
            document.addEventListener('visibilitychange', checkForUpdate);
            window.addEventListener('online', checkForUpdate);
            register();
        }
    }
    notify();
    return id;
}

export function unsubscribe(id) {
    subscribers.delete(id);
}

export async function clearAppCache(confirmation) {
    if (clearing || !window.confirm(confirmation)) return;
    clearing = true;
    try {
        const base = new URL('./', document.baseURI);
        // 目录请求不命中 worker 的资源列表；先确认网络可达，避免离线清理后无法启动。
        const response = await fetch(base, { cache: 'no-store', signal: AbortSignal.timeout(10000) });
        if (!response.ok) throw new Error('App network check failed');
        if ('serviceWorker' in navigator) {
            const registrations = await navigator.serviceWorker.getRegistrations();
            for (const item of registrations) {
                if (item.scope === base.href && !await item.unregister()) {
                    throw new Error('App worker could not be unregistered');
                }
            }
        }
        if ('caches' in window) {
            const prefix = `cc-offline-${encodeURIComponent(base.pathname)}-`;
            const indexUrl = new URL('index.html', base).href;
            for (const key of await caches.keys()) {
                let belongsToApp = key.startsWith(prefix);
                if (!belongsToApp && key.startsWith('offline-cache-')) {
                    const legacy = await caches.open(key);
                    belongsToApp = Boolean(await legacy.match(indexUrl));
                }
                if (belongsToApp) await caches.delete(key);
            }
        }
        // 不清空 localStorage，也不触碰同域其他应用的缓存或注册。
        window.location.reload();
    } catch (error) {
        clearing = false;
        throw error;
    }
}
