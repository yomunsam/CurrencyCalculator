# Currency Calculator

一个基于 **Blazor WebAssembly + PWA + .NET 10** 的静态部署汇率计算器。

## 特性

- 🌓 Dark Mode / Light Mode（自动检测系统主题，可手动切换）
- 💱 多币种对比（2~5 项），焦点行为基准自动换算
- 🪙 13 法币 + 2 加密币（USD、CNY、EUR、JPY、GBP、HKD、MOP、SGD、AUD、CAD、CHF、NZD、TWD、BTC、ETH）
- ⚡ 实时汇率优先，失败逐级回退（缓存 → 静态 fallback）
- ⏰ 过期检测：法币 >24h、加密 >1h 显示警告
- 🔢 输入支持数学表达式（如 `100*1.2`、`(50+30)/3`）
- 🌐 en-US / zh-CN 双语，浏览器语言自动匹配
- 💾 localStorage 持久化偏好与汇率缓存
- 🧩 Home 页面 UI 已拆分为可复用组件（货币行、选择浮层、弹窗）
- ↕️ 货币项支持长按拖拽排序，并记忆排序与最近选择偏好
- 📱 响应式紧凑卡片布局，移动端友好
- 🔧 数据驱动架构 — 新增币种/语言只需添加一行配置

## 项目结构

- `Src/CurrencyCalculator.slnx`：解决方案
- `Src/CurrencyCalculator.Web`：Blazor WebAssembly PWA 项目
  - `Core/`：领域模型（CurrencyCatalog、AppSettings、MathExpressionEvaluator）
  - `Models/`：状态模型（CompareItemState、UserPreferences、ExchangeRatesSnapshot）
  - `Services/`：业务服务（LocalizationService、BrowserStorageService）
  - `Services/Rates/`：汇率数据源与编排
  - `Pages/Home.razor`：页面状态编排
  - `Pages/*.razor`：可复用 UI 组件（货币行、Selector Overlay、Modal）
- `.github/workflows/`：GitHub Actions（部署 + fallback 更新）
- `Docs/`：项目文档

## 本地运行

```bash
dotnet restore Src/CurrencyCalculator.slnx
dotnet run --project Src/CurrencyCalculator.Web/CurrencyCalculator.Web.csproj
```

## 部署

### GitHub Pages

1. 在仓库 Settings → Pages 中启用 `GitHub Actions`。
2. 推送到 `main` 分支即可触发自动部署。

### Cloudflare Pages

- 构建命令：

```bash
dotnet publish Src/CurrencyCalculator.Web/CurrencyCalculator.Web.csproj -c Release -o publish
```

- 输出目录：`publish/wwwroot`
- 缓存控制：发布产物中的 `wwwroot/_headers` 会为 `service-worker.js`、`service-worker-assets.js`、`index.html` 和 `manifest.webmanifest` 下发禁缓存头，避免 Cloudflare Pages 边缘缓存放大 PWA 版本错配问题。

## fallback 汇率更新

- 工作流：`.github/workflows/update-fallback-rates.yml`
- 计划：每周一次（UTC 周一 02:00）
- 脚本：`.github/scripts/update-fallback.ps1`
- 输出：`Src/CurrencyCalculator.Web/wwwroot/fallback/latest-rates.json`

## 扩展指南

| 操作 | 位置 | 说明 |
|------|------|------|
| 新增货币 | `Core/CurrencyCatalog.cs` | 添加一行 `Fiat()/Crypto()` 定义 |
| 快速新增货币指引 | `Docs/新增货币类型指南.md` | 包含代码、fallback、验证完整流程 |
| 新增语言 | `Services/LocalizationService.cs` | 添加 `LanguageEntry` + 翻译字典 |
| 新增数据源 | 实现 `IExchangeRateProvider` | 在 `Program.cs` 注册即可 |
| 调整阈值 | `Core/AppSettings.cs` | 对比项上限、过期时间等 |
