using System.Text.Json;
using CurrencyCalculator.Web.Core;
using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web.Services.Rates;

public sealed class FawazExchangeApiProvider(HttpClient httpClient, ILogger<FawazExchangeApiProvider> logger) : IExchangeRateProvider
{
    private const string Url = "https://latest.currency-api.pages.dev/v1/currencies/usd.json";

    public string Name => "fawaz.exchange-api";
    public bool IsFallbackProvider => false;

    public async Task<ProviderFetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(Url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if (!root.TryGetProperty("usd", out var usdRates) || usdRates.ValueKind != JsonValueKind.Object)
            {
                return ProviderFetchResult.Fail("Response does not contain usd rates object.");
            }

            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["USD"] = 1m
            };

            foreach (var property in usdRates.EnumerateObject())
            {
                var code = property.Name.ToUpperInvariant();
                if (!CurrencyCatalog.IsSupported(code))
                {
                    continue;
                }

                if (property.Value.ValueKind is JsonValueKind.Number && property.Value.TryGetDecimal(out var value))
                {
                    rates[code] = value;
                }
            }

            if (rates.Count <= 1)
            {
                return ProviderFetchResult.Fail("No supported rates found in fawaz response.");
            }

            // Always use current time for staleness detection.
            // The API 'date' field is the rate reference date, not fetch time.
            return ProviderFetchResult.Ok(new ExchangeRatesSnapshot
            {
                BaseCurrency = "USD",
                FetchedAtUtc = DateTimeOffset.UtcNow,
                Source = Name,
                SourceKind = ExchangeRatesSourceKind.LiveApi,
                RatesFromBase = rates
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch rates from {Provider}", Name);
            return ProviderFetchResult.Fail(ex.Message);
        }
    }
}
