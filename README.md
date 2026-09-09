# Currency Calculator

基于 .NET 10、Blazor WebAssembly 的汇率计算器 PWA，纯静态部署，无需后端。

- 同时对比 2–8 个币种，以当前活动行为基准换算，支持数学表达式和拖动排序。
- 支持 15 种法币：USD、CNY、EUR、JPY、GBP、HKD、MOP、SGD、AUD、CAD、CHF、NZD、TWD、PHP（菲律宾比索）、KRW（韩元），以及 BTC、ETH。
- 英文、简体中文、日文；亮暗主题；本地保存偏好与汇率缓存。
- 货币选择器支持代码、当前语言和英文名称即时搜索，按最近使用、法币、加密币分组。
- 清空按钮固定；触控左滑展开删除，右键或拖柄菜单提供删除与上移下移，拖柄支持直接排序。
- 启动先显示本地缓存或静态 fallback，再尝试 fawaz exchange-api、Frankfurter（仅部分法币）；每个在线源最多等待 4 秒。缺失汇率明确显示不可用，法币超过 24 小时、加密币超过 1 小时提示过期。
- 发布版缓存完整应用快照（含本地图标字体），完成首次缓存后可离线使用。“关于”显示离线准备与更新状态；新版本等待本站所有标签页及已安装应用窗口关闭后生效。开发服务器不提供同等离线体验。

## 本地运行

需要 .NET 10 SDK。

```sh
dotnet run --project Src/CurrencyCalculator.Web/CurrencyCalculator.Web.csproj --launch-profile http
```

默认地址为 http://localhost:5085。构建命令：

```sh
dotnet build Src/CurrencyCalculator.slnx
```

## 代码位置

| 位置 | 用途 |
| --- | --- |
| `Src/CurrencyCalculator.Web/Core/` | 币种目录、计算表达式、应用配置 |
| `Src/CurrencyCalculator.Web/Models/` | 对比项、偏好和汇率快照 |
| `Src/CurrencyCalculator.Web/Services/` | 国际化、浏览器存储与环境 |
| `Src/CurrencyCalculator.Web/Services/Rates/` | 汇率数据源与回退编排 |
| `Src/CurrencyCalculator.Web/Pages/` | 页面状态与设置等弹窗 |
| `Src/CurrencyCalculator.Web/Components/Currencies/` | 货币列表、货币行与搜索选择器 |
| `Src/CurrencyCalculator.Web/wwwroot/` | 样式、图标、PWA 与静态汇率 |
| `.github/` | 部署与 fallback 更新脚本 |

开发约定见 [AGENTS.md](AGENTS.md)，币种扩展见 [新增货币类型指南](Docs/新增货币类型指南.md)。`Docs/` 中初始需求、里程碑和架构决策保留为历史记录，可能与当前实现不同。

事件与 PWA 回归检查使用 Node.js 22：`node --test Tests/Interactions/*.test.mjs Tests/Pwa/*.test.mjs`。算式检查为 `dotnet run --project Tests/Core/ExpressionChecks.csproj`，汇率与存储检查为 `dotnet run --project Tests/Pwa/RateChecks.csproj`。浏览器和真机验收边界见 [交互验收](Docs/交互翻新验收.md) 与 [PWA 验收](Docs/PWA验收.md)。

## 发布与维护

```sh
dotnet publish Src/CurrencyCalculator.Web/CurrencyCalculator.Web.csproj -c Release -o publish
```

静态站点输出为 `publish/wwwroot`。现有 GitHub Actions 在推送 `master` 时部署：

- **GitHub Pages**：仓库 Pages 设置选择 GitHub Actions；工作流调整仓库子路径的 base href，同步更新入口哈希、缓存版本和压缩副本，并逐项验证最终资源清单。
- **Cloudflare Pages**：配置 `CLOUDFLARE_ACCOUNT_ID`、`CLOUDFLARE_API_TOKEN`，以及工作流内的 `CF_PROJECT_NAME`。`wwwroot/_headers` 控制入口与 PWA 更新文件的缓存。

fallback 工作流每周一 UTC 02:00 更新，也可在仓库根目录手动运行：

```powershell
pwsh .github/scripts/update-fallback.ps1
```

生成文件为 `Src/CurrencyCalculator.Web/wwwroot/fallback/latest-rates.json`。增加币种时应同步更新生成脚本并重新生成数据，不能只增加选择项。

## PWA 使用边界

- 手机安装与 Service Worker 需要 HTTPS；`localhost` 可供本机验证。局域网 `http://192.168.*` 可测试页面交互，但不能验证安装和离线缓存。
- 首次在线打开后，在“关于”确认离线文件已就绪，再测试断网启动。不能将安装图标已出现当成缓存已完成。
- 新版本下载完成不会强制接管正在编辑的页面。看到更新就绪后，关闭本站所有标签页及已安装应用窗口再打开；仅刷新一个仍被旧 worker 控制的页面可能继续使用旧版本。
- 浏览器禁止存储、容量已满或本地 JSON 损坏不会阻止基础计算。无法持久保存时，“关于”给出说明；离线快照仍保留来源和实际时间，不代表实时价格。
- 离线 fallback 属于发布时的快照。每周生成的数据可能早于当天，须以界面时间和过期提示为准；本轮未更改 fallback 调度与自动部署规则。
