# PWA 检查与验收

## 本轮修正

1. 发布 worker 不再在安装时调用 `skipWaiting` 或在激活时强制 `clients.claim`。导航和静态资源读取同一发布快照，避免网络返回新 HTML 而程序集仍来自旧缓存。新版本在旧窗口全部关闭后自然激活。
2. 资源地址与缓存名称按 worker 所在目录区分，根路径与 GitHub Pages 子路径使用同一逻辑。安装失败不删除旧版；激活仅清理本应用旧缓存。
3. Bootstrap Icons 1.11.3 的 CSS、WOFF/WOFF2 和 MIT 许可放入本地目录。离线缓存包含字体，应用启动不再依赖图标 CDN。发布调试符号不加入离线缓存。
4. Service Worker 仅在安全上下文及支持的浏览器中注册。注册失败有明确状态；“关于”显示开发预览、离线准备、就绪与待更新状态。回到前台或恢复网络时检查更新，并限制重复检查频率。
5. 启动优先显示本地快照，后台尝试在线更新；每个在线源限时 4 秒，同源 fallback 限时 2 秒。在线失败使用旧数据时不再提示“更新成功”。
6. 非 USD、损坏、零值或缺失汇率不被当作有效换算数据。部分法币源不再请求不支持的币种；缺失结果显示不可用，并禁用该换算结果的复制。
7. 存储被禁用、写入失败或 JSON 损坏不再中断基础启动。主题同样走安全存储入口，不能保存时在“关于”说明。
8. 安装清单允许横竖屏；移除禁止缩放设置，保留安全区域适配。减少动画模式下，提示消息也能正确移除。
9. GitHub Pages 的发布后修改使用结构化脚本，更新 index 哈希、清单版本以及已有 gzip/Brotli 副本。两个部署流程均运行回归检查与全部资源哈希验证。

## 本地验证命令

```sh
node --test Tests/Interactions/*.test.mjs Tests/Pwa/*.test.mjs
dotnet run --project Tests/Core/ExpressionChecks.csproj
dotnet run --project Tests/Pwa/RateChecks.csproj
dotnet publish Src/CurrencyCalculator.Web/CurrencyCalculator.Web.csproj -c Release -o publish
node .github/scripts/pwa-artifacts.mjs publish/wwwroot
```

GitHub Pages 模式另运行 `node .github/scripts/pwa-artifacts.mjs publish/wwwroot /CurrencyCalculator/`，会修改该发布目录。

## 已验证与未验证

本轮使用真实 Release 产物验证资源清单与哈希，并以 Node 模拟 Service Worker 的安装、激活、导航和浏览器能力边界；C# 检查覆盖缓存损坏、USD 基准、无效汇率、超时及调用方取消。上述检查不能证明浏览器实际完成缓存、安装或离线启动。

按用户要求，本轮不做浏览器交互测试。以下需在实际 HTTPS 发布站点复测：

1. 首次在线打开，“关于”从准备变为就绪；安装后断网冷启动，图标、主题、货币选择、已保存金额和过期提示可用。
2. 有本地快照时切换弱网，立即显示旧汇率并可编辑；在线失败不误报刷新成功。缺少某币种时显示不可用，不能复制旧换算值冒充新结果。
3. 保持旧版打开后部署新版本：当前算式不丢失，状态提示更新就绪；关闭本站所有页面与 PWA 窗口再打开，整套资源切换到新版。
4. 分别在根路径站点和 GitHub Pages 子路径测试带查询参数的断网打开，确认都返回当前应用入口。
5. 系统禁止存储或容量已满时，仍能打开和计算，且保存限制可见。删除单条损坏偏好后不影响其他数据。
6. 折叠屏旋转、安装模式、双指缩放、安全区域与系统键盘逃逸，确认没有回退到固定竖屏。

## 发布与数据限制

- 局域网 HTTP 开发预览不能验证手机 Service Worker 和安装行为，必须使用 HTTPS 发布地址。
- 静态 fallback 随应用版本缓存，不是在线更新成功的替代品；现有每周生成任务不保证部署版数据每日变化，本轮不改变调度。
- 本轮只验证本地发布产物与推送；部署是否完成以 GitHub Actions 的实际结果为准。

更新生命周期依据 [Blazor PWA 官方文档](https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app/?view=aspnetcore-10.0) 和 [Service Worker 生命周期说明](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API/Using_Service_Workers)。
