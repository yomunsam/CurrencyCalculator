# API 调研结论

## 目标

验证无 API Key 条件下，客户端与 GitHub Actions 是否可共用同一汇率来源，并满足法币 + BTC/ETH 的需求。

## 结论

1. **fawazahmed0/exchange-api 可作为首选**
   - 可匿名访问
   - 数据格式简单，适合客户端直接拉取
   - 覆盖法币与常见加密币（含 BTC/ETH）
   - 适合作为客户端实时汇率源与 GitHub Actions 更新源

2. **Frankfurter 适合做次级兜底（法币）**
   - 稳定、无 Key
   - 对法币支持良好
   - 不适合作为 BTC/ETH 主数据源

3. **推荐组合**
   - 客户端：fawaz（主） -> Frankfurter（法币次级） -> localStorage -> static fallback
   - Actions：优先 fawaz 定时更新 fallback 文件

## 风险与应对

- 风险：第三方接口结构变化或短时不可用
- 应对：
  - 通过 `IExchangeRateProvider` 抽象快速替换数据源
  - 保留静态 fallback 与缓存策略
  - 在工作流中保持低频更新，减少接口压力与变更风险
