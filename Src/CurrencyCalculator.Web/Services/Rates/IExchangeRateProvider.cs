using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web.Services.Rates;

public interface IExchangeRateProvider
{
    string Name { get; }
    bool IsFallbackProvider { get; }
    Task<ProviderFetchResult> FetchAsync(CancellationToken cancellationToken = default);
}

public sealed record ProviderFetchResult(
    bool Success,
    ExchangeRatesSnapshot? Snapshot,
    string? ErrorMessage)
{
    public static ProviderFetchResult Ok(ExchangeRatesSnapshot snapshot)
        => new(true, snapshot, null);

    public static ProviderFetchResult Fail(string errorMessage)
        => new(false, null, errorMessage);
}
