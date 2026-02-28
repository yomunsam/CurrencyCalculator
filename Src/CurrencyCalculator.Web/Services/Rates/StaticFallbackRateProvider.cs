using System.Net.Http.Json;
using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web.Services.Rates;

/// <summary>
/// Last-resort provider that reads a static JSON file (wwwroot/fallback/latest-rates.json)
/// pre-built and committed by a scheduled GitHub Actions workflow.
/// Guaranteed to be available offline, but rates may be days or weeks old.
/// </summary>
public sealed class StaticFallbackRateProvider(HttpClient httpClient, ILogger<StaticFallbackRateProvider> logger) : IExchangeRateProvider
{
    public string Name => "Static Fallback";
    public bool IsFallbackProvider => true;

    public async Task<ProviderFetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await httpClient.GetFromJsonAsync<ExchangeRatesSnapshot>("fallback/latest-rates.json", cancellationToken);
            if (snapshot is null || snapshot.RatesFromBase.Count == 0)
            {
                return ProviderFetchResult.Fail("Fallback file is empty.");
            }

            return ProviderFetchResult.Ok(new ExchangeRatesSnapshot
            {
                BaseCurrency = snapshot.BaseCurrency,
                FetchedAtUtc = snapshot.FetchedAtUtc,
                Source = Name,
                SourceKind = ExchangeRatesSourceKind.StaticFallback,
                RatesFromBase = snapshot.RatesFromBase
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read static fallback rates");
            return ProviderFetchResult.Fail(ex.Message);
        }
    }
}
