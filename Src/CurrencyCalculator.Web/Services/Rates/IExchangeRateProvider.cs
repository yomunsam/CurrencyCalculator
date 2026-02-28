using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web.Services.Rates;

/// <summary>
/// Contract for exchange rate data providers.
/// Implementations include live API providers (fawazahmed0, Frankfurter)
/// and the static fallback JSON provider.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>Human-readable name for logging.</summary>
    string Name { get; }
    /// <summary>True for providers that serve pre-built data (e.g. static JSON), false for live APIs.</summary>
    bool IsFallbackProvider { get; }
    /// <summary>Fetch rates and return a result envelope.</summary>
    Task<ProviderFetchResult> FetchAsync(CancellationToken cancellationToken = default);
}

/// <summary>Result envelope returned by <see cref="IExchangeRateProvider.FetchAsync"/>.</summary>
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
