# AGENT.md

## 目标

该项目是一个纯前端静态部署的汇率计算器 PWA，优先保证：

1. 可靠的离线/弱网可用性
2. 清晰可扩展的数据驱动架构
3. 快速迭代而不破坏已有行为
4. 原生 App 般的紧凑交互体验

## 代码约定

- 使用 .NET 10 / C# 最新语法
- 业务模型放在 `Core` 与 `Models`
- 汇率相关服务放在 `Services/Rates`
- `IExchangeRateProvider` 统一数据源扩展接口
- UI 逻辑集中在 `Pages/Home.razor`
- CSS 变量驱动的亮/暗主题（`[data-bs-theme]`）

## 架构要点

### 数据驱动币种

`CurrencyDefinition` 包含 `Icon`（emoji）、`DisplayDecimals`、`Names`（多语言名称字典）。
新增币种只需在 `CurrencyCatalog` 添加一行定义。

### 数据驱动国际化

`LocalizationService` 使用 `LanguageEntry` 记录，包含语言代码、显示名称、默认币种。
新增语言只需添加一条 `LanguageEntry` + 一份翻译字典。

### 输入增强

- 支持数学表达式（`100*1.2`、`(50+30)/3`），由 `MathExpressionEvaluator` 递归下降解析
- 编辑态显示原始输入，失焦后求值格式化

## 设计原则

- Online-first，失败后回退到 local cache，再回退到 static fallback
- 页面加载时始终先尝试获取最新汇率（`forceRefresh: true`）
- 所有支持币种必须由 `CurrencyCatalog` 统一维护
- 不引入额外组件库，UI 基于 Bootstrap 5.3+ 与自定义 CSS 变量
- 任何功能扩展优先新增服务，不在 Razor 页面堆叠复杂逻辑

## 数据源策略

- Primary: fawaz exchange-api（覆盖法币与加密币）
- Secondary: Frankfurter（法币兜底）
- Fallback: `wwwroot/fallback/latest-rates.json`

## 维护提醒

- 修改支持币种后，记得同步更新 `.github/scripts/update-fallback.ps1`
- 新增语言：在 `LocalizationService` 中添加 `LanguageEntry` 和翻译字典
- 新增币种：在 `CurrencyCatalog` 中添加定义行，无需改动 UI
- 过期阈值：法币 24h、加密 1h，配置在 `AppSettings`
