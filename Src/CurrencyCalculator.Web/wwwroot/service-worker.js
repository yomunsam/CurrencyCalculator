// 开发环境不缓存资源，离线验收必须使用 Release 发布产物。
self.addEventListener('fetch', () => {});
self.addEventListener('message', event => {
    if (event.data?.type === 'GET_OFFLINE_STATUS') {
        event.ports[0]?.postMessage({ offlineReady: false, development: true });
    }
});
