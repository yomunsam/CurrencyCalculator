# Currency Calculator

一个基于 **Blazor WebAssembly + PWA + .NET 10** 的静态部署汇率计算器项目。

## 当前能力

- 多币种对比（默认 2 项，可扩展到 5 项）
- 支持法币与加密货币（USD、CNY、EUR、JPY、GBP、HKD、AUD、CAD、CHF、NZD、BTC、ETH）
- 以当前输入焦点行为基准货币进行换算
- 实时汇率优先（fawaz exchange-api，Frankfurter 兜底）
- 本地缓存与静态 fallback 文件双重回退
- `en-US` / `zh-CN` 双语，首次访问按浏览器语言设置默认币种
- `localStorage` 持久化用户对比项、历史输入与语言偏好

## 项目结构

- `Src/CurrencyCalculator.slnx`：解决方案
- `Src/CurrencyCalculator.Web`：Blazor WebAssembly PWA 项目
- `.github/workflows/update-fallback-rates.yml`：低频更新 fallback 汇率
- `.github/workflows/deploy-github-pages.yml`：GitHub Pages 部署
- `Docs/项目计划与里程碑.md`：计划与阶段说明
- `Docs/架构决策记录.md`：关键架构决策

## 本地运行

```bash
dotnet restore Src/CurrencyCalculator.slnx
dotnet run --project Src/CurrencyCalculator.Web/CurrencyCalculator.Web.csproj
```

## 部署说明

### GitHub Pages

1. 在仓库 Settings > Pages 中启用 `GitHub Actions`。
2. 确保默认分支为 `main`，推送后触发 `Deploy Blazor WASM to GitHub Pages`。

### Cloudflare Pages

- 构建命令：

```bash
dotnet publish Src/CurrencyCalculator.Web/CurrencyCalculator.Web.csproj -c Release -o publish
```

- 输出目录：`publish/wwwroot`

## fallback 汇率更新

- 工作流：`.github/workflows/update-fallback-rates.yml`
- 默认计划：每周一次（UTC 周一 02:00）
- 脚本：`.github/scripts/update-fallback.ps1`
- 输出：`Src/CurrencyCalculator.Web/wwwroot/fallback/latest-rates.json`

## 可扩展点

- 新增货币：编辑 `Src/CurrencyCalculator.Web/Core/CurrencyCatalog.cs`
- 新增数据源：实现 `IExchangeRateProvider` 并在 `Program.cs` 注册
- 调整对比项上限：编辑 `Src/CurrencyCalculator.Web/Core/AppSettings.cs`
