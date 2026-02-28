using CurrencyCalculator.Web.Core;
using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web.Services.Rates;

public sealed class ExchangeRateService(
    IEnumerable<IExchangeRateProvider> providers,
    BrowserStorageService browserStorageService,
    ILogger<ExchangeRateService> logger)
{
    public async Task<ExchangeRatesSnapshot> GetLatestAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToArray();
        var onlineProviders = providerList.Where(provider => !provider.IsFallbackProvider).ToArray();
        var fallbackProviders = providerList.Where(provider => provider.IsFallbackProvider).ToArray();

        if (!forceRefresh)
        {
            var freshCache = await TryLoadCacheAsync(enforceTtl: true);
            if (freshCache is not null)
            {
                return freshCache;
            }
        }

        foreach (var provider in onlineProviders)
        {
            var result = await provider.FetchAsync(cancellationToken);
            if (!result.Success || result.Snapshot is null)
            {
                continue;
            }

            var normalized = EnsureSupportedRates(result.Snapshot);
            if (!normalized.HasAllRates(CurrencyCatalog.SupportedCodes))
            {
                logger.LogInformation("Provider {Provider} did not return full supported currency set.", provider.Name);
            }

            await browserStorageService.SetAsync(AppSettings.RatesCacheStorageKey, normalized);
            return normalized;
        }

        var anyCache = await TryLoadCacheAsync(enforceTtl: false);
        if (anyCache is not null)
        {
            return anyCache;
        }

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
