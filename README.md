# Currency Calculator

基于 .NET 10、Blazor WebAssembly 的汇率计算器 PWA，纯静态部署，无需后端。

- 同时对比 2–5 个币种，以当前活动行为基准换算，支持数学表达式和拖动排序。
- 支持 15 种法币：USD、CNY、EUR、JPY、GBP、HKD、MOP、SGD、AUD、CAD、CHF、NZD、TWD、PHP（菲律宾比索）、KRW（韩元），以及 BTC、ETH。
- 英文、简体中文、日文；亮暗主题；本地保存偏好与汇率缓存。
- 货币选择器支持代码、当前语言和英文名称即时搜索，按最近使用、法币、加密币分组。
- 清空按钮固定；触控左滑展开删除，右键或拖柄菜单提供删除与上移下移，拖柄支持直接排序。
- 在线数据源依次为 fawaz exchange-api、Frankfurter（仅部分法币），失败后使用本地缓存、静态 fallback。法币超过 24 小时、加密币超过 1 小时提示过期。
- 发布版通过 Service Worker 缓存应用资源，完成首次缓存后可离线使用。开发服务器不提供同等离线体验。

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

指针交互回归检查使用 Node.js 内置测试运行器：`node --test Tests/Interactions/*.test.mjs`。浏览器和真机验收边界见 [本轮交互验收](Docs/交互翻新验收.md)。

## 发布与维护

```sh
dotnet publish Src/CurrencyCalculator.Web/CurrencyCalculator.Web.csproj -c Release -o publish
```

静态站点输出为 `publish/wwwroot`。现有 GitHub Actions 在推送 `master` 时部署：

- **GitHub Pages**：仓库 Pages 设置选择 GitHub Actions；工作流调整仓库子路径的 base href，并更新 Service Worker 清单中的 index.html 哈希。
- **Cloudflare Pages**：配置 `CLOUDFLARE_ACCOUNT_ID`、`CLOUDFLARE_API_TOKEN`，以及工作流内的 `CF_PROJECT_NAME`。`wwwroot/_headers` 控制入口与 PWA 更新文件的缓存。

fallback 工作流每周一 UTC 02:00 更新，也可在仓库根目录手动运行：

```powershell
pwsh .github/scripts/update-fallback.ps1
```

生成文件为 `Src/CurrencyCalculator.Web/wwwroot/fallback/latest-rates.json`。增加币种时应同步更新生成脚本并重新生成数据，不能只增加选择项。
