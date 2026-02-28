using System.Text.Json;
using CurrencyCalculator.Web.Core;
using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web.Services.Rates;

/// <summary>
/// Secondary / fallback live provider. Fetches fiat-only rates from the Frankfurter API
/// (ECB data). Does not provide crypto rates — those come from the primary provider.
/// </summary>
public sealed class FrankfurterExchangeRateProvider(HttpClient httpClient, ILogger<FrankfurterExchangeRateProvider> logger) : IExchangeRateProvider
{
    public string Name => "Frankfurter";
    public bool IsFallbackProvider => false;

    public async Task<ProviderFetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var symbols = string.Join(',', CurrencyCatalog.FiatCodes.Where(code => !code.Equals("USD", StringComparison.OrdinalIgnoreCase)));
            var requestUrl = $"https://api.frankfurter.app/latest?from=USD&to={symbols}";

            using var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if (!root.TryGetProperty("rates", out var ratesObject) || ratesObject.ValueKind != JsonValueKind.Object)
            {
                return ProviderFetchResult.Fail("Response does not contain rates object.");
            }

            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["USD"] = 1m
            };

            foreach (var property in ratesObject.EnumerateObject())
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
                return ProviderFetchResult.Fail("No supported rates found in Frankfurter response.");
            }

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
