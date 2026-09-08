using System.Diagnostics;
using System.Text.Json;
using CurrencyCalculator.Web.Core;
using CurrencyCalculator.Web.Models;
using CurrencyCalculator.Web.Services;
using CurrencyCalculator.Web.Services.Rates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

var stored = new MemoryJs();
var storage = new BrowserStorageService(stored);
var oldTime = DateTimeOffset.UtcNow.AddDays(-3);
ExchangeRatesSnapshot Snapshot(string basis = "USD", decimal usd = 1m) => new()
{
    BaseCurrency = basis, FetchedAtUtc = oldTime, Source = "Original Provider",
    SourceKind = ExchangeRatesSourceKind.LiveApi,
    RatesFromBase = new() { ["USD"] = usd, ["CNY"] = 7m, ["EUR"] = 0m }
};
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
ExchangeRateService Service(params IExchangeRateProvider[] providers) =>
    new(providers, storage, NullLogger<ExchangeRateService>.Instance);

await storage.SetAsync(AppSettings.RatesCacheStorageKey, Snapshot());
var online = new FakeProvider(false, _ => throw new Exception("启动不应等待在线源"));
var cached = await Service(online).GetAvailableAsync();
Check(cached?.SourceKind == ExchangeRatesSourceKind.LocalCache, "启动应显示已有缓存");
Check(cached!.FetchedAtUtc == oldTime && cached.Source == "Original Provider", "缓存不能伪造更新时间或丢失来源");
Check(!cached.HasRate("EUR"), "零汇率不可参与换算");

stored.Values[AppSettings.RatesCacheStorageKey] = "{broken";
var fallback = new FakeProvider(true, _ => Task.FromResult(ProviderFetchResult.Ok(Snapshot())));
Check(await Service(fallback).GetAvailableAsync() is not null, "损坏缓存不能阻止静态回退");
stored.Values[AppSettings.PreferencesStorageKey] = "invalid";
Check(await storage.GetAsync<UserPreferences>(AppSettings.PreferencesStorageKey) is null, "损坏偏好应恢复默认读取");

await storage.SetAsync(AppSettings.RatesCacheStorageKey, Snapshot("EUR"));
Check(await Service().GetAvailableAsync() is null, "不能把其他基准货币冒充 USD");
await storage.SetAsync(AppSettings.RatesCacheStorageKey, Snapshot(usd: 0m));
Check(await Service().GetAvailableAsync() is null, "缓存中 USD 必须为 1");

stored.Values.Clear();
var timedOut = false;
var stalled = new FakeProvider(false, async token =>
{
    try { await Task.Delay(Timeout.Infinite, token); }
    catch (OperationCanceledException) { timedOut = true; throw; }
    return ProviderFetchResult.Fail("unreachable");
});
var working = new FakeProvider(false, _ => Task.FromResult(ProviderFetchResult.Ok(Snapshot())));
var timer = Stopwatch.StartNew();
var live = await Service(stalled, working).GetLatestAsync(forceRefresh: true);
Check(timedOut && timer.Elapsed < TimeSpan.FromSeconds(7), "超时后应继续尝试下一个源");
Check(live.HasRate("CNY") && !live.HasRate("EUR"), "在线数据同样要过滤无效汇率");

using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
try
{
    await Service(working).GetLatestAsync(forceRefresh: true, cancellationToken: cancelled.Token);
    throw new InvalidOperationException("调用方取消不能被转换成回退成功");
}
catch (OperationCanceledException) { }

// 存储写入失败不应丢弃刚取得的有效在线汇率。
stored.IgnoreWrites = true;
Check((await Service(working).GetLatestAsync(forceRefresh: true)).HasRate("CNY"), "无法持久化时仍可计算");
Console.WriteLine("11 项汇率和存储边界检查通过。");

sealed class FakeProvider(bool fallback, Func<CancellationToken, Task<ProviderFetchResult>> fetch) : IExchangeRateProvider
{
    public string Name => "Test Provider";
    public bool IsFallbackProvider => fallback;
    public Task<ProviderFetchResult> FetchAsync(CancellationToken cancellationToken = default) => fetch(cancellationToken);
}

sealed class MemoryJs : IJSRuntime
{
    public Dictionary<string, string> Values { get; } = [];
    public bool IgnoreWrites { get; set; }
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, default, args);
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        var key = (string)args![0]!;
        if (identifier == "ccStorage.get") return ValueTask.FromResult((TValue)(object?)Values.GetValueOrDefault(key)!);
        if (identifier == "ccStorage.set" && !IgnoreWrites) Values[key] = (string)args[1]!;
        return ValueTask.FromResult(default(TValue)!);
    }
}
