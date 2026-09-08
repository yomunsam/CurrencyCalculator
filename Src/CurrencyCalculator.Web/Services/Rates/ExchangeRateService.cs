using CurrencyCalculator.Web.Core;
using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web.Services.Rates;

/// <summary>协调在线汇率和本地快照，始终保留来源及原始时间戳。</summary>
public sealed class ExchangeRateService(
    IEnumerable<IExchangeRateProvider> providers,
    BrowserStorageService browserStorageService,
    ILogger<ExchangeRateService> logger)
{
    /// <summary>启动先显示已有数据，不等待外部 API 超时。</summary>
    public async Task<ExchangeRatesSnapshot?> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cached = await TryLoadCacheAsync(enforceTtl: false);
        if (cached is not null) return cached;
        foreach (var provider in providers.Where(provider => provider.IsFallbackProvider))
        {
            var snapshot = await FetchWithTimeoutAsync(provider, AppSettings.LocalProviderTimeout, cancellationToken);
            if (snapshot is not null) return snapshot;
        }
        return null;
    }

    public async Task<ExchangeRatesSnapshot> GetLatestAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!forceRefresh)
        {
            var cached = await TryLoadCacheAsync(enforceTtl: true);
            if (cached is not null) return cached;
        }
        foreach (var provider in providers.Where(provider => !provider.IsFallbackProvider))
        {
            var snapshot = await FetchWithTimeoutAsync(provider, AppSettings.OnlineProviderTimeout, cancellationToken);
            if (snapshot is null) continue;
            await browserStorageService.SetAsync(AppSettings.RatesCacheStorageKey, snapshot);
            return snapshot;
        }
        return await GetAvailableAsync(cancellationToken)
            ?? throw new InvalidOperationException("No exchange rate source is currently available.");
    }

    private async Task<ExchangeRatesSnapshot?> FetchWithTimeoutAsync(
        IExchangeRateProvider provider, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bounded.CancelAfter(timeout);
        try
        {
            var result = await provider.FetchAsync(bounded.Token);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Success && result.Snapshot is not null)
                return Normalize(result.Snapshot);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Rate provider {Provider} timed out.", provider.Name);
        }
        return null;
    }

    /// <summary>
    /// Convert an amount between two currencies using the USD-based cross-rate:
    ///   result = amount / rate(from) * rate(to).
    /// This is the standard approach for consumer-grade FX tools and introduces
    /// negligible error (&lt;0.01%) compared to direct pair rates, because the
    /// fawazahmed0 API derives all rates from USD anyway.
    /// </summary>
    public decimal Convert(decimal amount, string fromCurrencyCode, string toCurrencyCode, ExchangeRatesSnapshot snapshot)
    {
        var fromCode = fromCurrencyCode.ToUpperInvariant();
        var toCode = toCurrencyCode.ToUpperInvariant();

        if (!snapshot.RatesFromBase.TryGetValue(fromCode, out var fromRate) || fromRate <= 0)
        {
            throw new InvalidOperationException($"Rate unavailable for {fromCode}");
        }

        if (!snapshot.RatesFromBase.TryGetValue(toCode, out var toRate) || toRate <= 0)
        {
            throw new InvalidOperationException($"Rate unavailable for {toCode}");
        }

        var amountInBase = amount / fromRate;
        var converted = amountInBase * toRate;
        return converted;
    }

    private async Task<ExchangeRatesSnapshot?> TryLoadCacheAsync(bool enforceTtl)
    {
        var stored = await browserStorageService.GetAsync<ExchangeRatesSnapshot>(AppSettings.RatesCacheStorageKey);
        if (stored is null) return null;
        var snapshot = Normalize(stored);
        if (snapshot is null) return null;
        if (enforceTtl && DateTimeOffset.UtcNow - snapshot.FetchedAtUtc > AppSettings.RatesCacheTtl) return null;
        return new ExchangeRatesSnapshot
        {
            BaseCurrency = snapshot.BaseCurrency,
            FetchedAtUtc = snapshot.FetchedAtUtc,
            Source = snapshot.Source,
            SourceKind = ExchangeRatesSourceKind.LocalCache,
            RatesFromBase = snapshot.RatesFromBase
        };
    }

    private static ExchangeRatesSnapshot? Normalize(ExchangeRatesSnapshot snapshot)
    {
        // 损坏的缓存或非 USD 快照不能被重新标注为 USD 后参与换算。
        if (!string.Equals(snapshot.BaseCurrency, "USD", StringComparison.OrdinalIgnoreCase)
            || snapshot.RatesFromBase is null || snapshot.FetchedAtUtc == default)
            return null;
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, value) in snapshot.RatesFromBase)
        {
            if (CurrencyCatalog.IsSupported(code) && value > 0) rates[code] = value;
        }
        if (rates.GetValueOrDefault("USD") != 1m || rates.Count < 2) return null;
        return new ExchangeRatesSnapshot
        {
            BaseCurrency = "USD",
            FetchedAtUtc = snapshot.FetchedAtUtc,
            Source = snapshot.Source,
            SourceKind = snapshot.SourceKind,
            RatesFromBase = rates
        };
    }
}
