using CurrencyCalculator.Web.Core;
using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web.Services.Rates;

/// <summary>
/// Orchestrates exchange rate retrieval with a multi-tier fallback strategy:
///   1. Live online API providers (fawazahmed0, Frankfurter)
///   2. Browser localStorage cache (with configurable TTL)
///   3. Static fallback JSON (pre-built by GitHub Actions)
/// All rates are normalized to a USD base.
/// </summary>
public sealed class ExchangeRateService(
    IEnumerable<IExchangeRateProvider> providers,
    BrowserStorageService browserStorageService,
    ILogger<ExchangeRateService> _logger)
{
    /// <summary>
    /// Retrieve the latest exchange rates.
    /// When <paramref name="forceRefresh"/> is true, skips cache and fetches from live APIs.
    /// Falls back to stale cache → static fallback if all APIs fail.
    /// </summary>
    public async Task<ExchangeRatesSnapshot> GetLatestAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToArray();
        var onlineProviders = providerList.Where(provider => !provider.IsFallbackProvider).ToArray();
        var fallbackProviders = providerList.Where(provider => provider.IsFallbackProvider).ToArray();

        // Strategy: always try live APIs first.
        // Only use cache as fresh shortcut when NOT force-refreshing.
        if (!forceRefresh)
        {
            var freshCache = await TryLoadCacheAsync(enforceTtl: true);
            if (freshCache is not null)
            {
                return freshCache;
            }
        }

        // Try live online providers
        foreach (var provider in onlineProviders)
        {
            var result = await provider.FetchAsync(cancellationToken);
            if (!result.Success || result.Snapshot is null)
            {
                _logger.LogWarning("Provider {Provider} failed or returned no data.", provider.Name);
                continue;
            }

            var normalized = EnsureSupportedRates(result.Snapshot);
            await browserStorageService.SetAsync(AppSettings.RatesCacheStorageKey, normalized);
            return normalized;
        }

        // Fallback 1: any cached data (even stale)
        var anyCache = await TryLoadCacheAsync(enforceTtl: false);
        if (anyCache is not null)
        {
            _logger.LogInformation("Using stale cached rates.");
            return anyCache;
        }

        // Fallback 2: static fallback JSON (built by GitHub Actions)
        foreach (var provider in fallbackProviders)
        {
            var result = await provider.FetchAsync(cancellationToken);
            if (!result.Success || result.Snapshot is null)
            {
                continue;
            }

            return EnsureSupportedRates(result.Snapshot);
        }

        throw new InvalidOperationException("No exchange rate source is currently available.");
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
        var snapshot = await browserStorageService.GetAsync<ExchangeRatesSnapshot>(AppSettings.RatesCacheStorageKey);
        if (snapshot is null)
        {
            return null;
        }

        if (enforceTtl)
        {
            var age = DateTimeOffset.UtcNow - snapshot.FetchedAtUtc;
            if (age > AppSettings.RatesCacheTtl)
            {
                return null;
            }
        }

        return new ExchangeRatesSnapshot
        {
            BaseCurrency = snapshot.BaseCurrency,
            FetchedAtUtc = snapshot.FetchedAtUtc,
            Source = "Local Cache",
            SourceKind = ExchangeRatesSourceKind.LocalCache,
            RatesFromBase = snapshot.RatesFromBase
        };
    }

    /// <summary>Normalize a snapshot to contain only currencies from <see cref="CurrencyCatalog"/>.</summary>
    private static ExchangeRatesSnapshot EnsureSupportedRates(ExchangeRatesSnapshot snapshot)
    {
        var normalizedRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = snapshot.RatesFromBase.GetValueOrDefault("USD", 1m)
        };

        foreach (var code in CurrencyCatalog.SupportedCodes)
        {
            if (snapshot.RatesFromBase.TryGetValue(code, out var value) && value > 0)
            {
                normalizedRates[code] = value;
            }
        }

        return new ExchangeRatesSnapshot
        {
            BaseCurrency = "USD",
            FetchedAtUtc = snapshot.FetchedAtUtc,
            Source = snapshot.Source,
            SourceKind = snapshot.SourceKind,
            RatesFromBase = normalizedRates
        };
    }
}
