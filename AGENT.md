# AGENT.md

## 目标

该项目是一个纯前端静态部署的汇率计算器，优先保证：

1. 可靠的离线/弱网可用性
2. 清晰可扩展的代码结构
3. 快速迭代而不破坏已有行为

## 代码约定

- 使用 .NET 10 / C# 最新语法
- 业务模型放在 `Core` 与 `Models`
- 汇率相关服务放在 `Services/Rates`
- `IExchangeRateProvider` 统一数据源扩展接口
- UI 逻辑集中在 `Pages/Home.razor`

## 设计原则

- Online-first，失败后回退到 local cache，再回退到 static fallback
- 所有支持币种必须由 `CurrencyCatalog` 统一维护
- 不引入额外组件库，UI 基于 Bootstrap 与自定义 CSS
- 任何功能扩展优先新增服务，不在 Razor 页面堆叠复杂逻辑

## 数据源策略

- Primary: fawaz exchange-api（覆盖法币与加密币）
- Secondary: Frankfurter（法币兜底）
- Fallback: `wwwroot/fallback/latest-rates.json`

## 维护提醒

- 修改支持币种后，记得同步更新 `.github/scripts/update-fallback.ps1`
- 如需新增语言，优先扩展 `LocalizationService`
- 任何 breaking change 需先更新 `Docs/架构决策记录.md`
