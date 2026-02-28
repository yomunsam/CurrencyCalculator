namespace CurrencyCalculator.Web.Models;

public enum ExchangeRatesSourceKind
{
    LiveApi,
    LocalCache,
    StaticFallback
}

public sealed class ExchangeRatesSnapshot
{
    public required string BaseCurrency { get; init; }
    public required DateTimeOffset FetchedAtUtc { get; init; }
    public required string Source { get; init; }
    public required ExchangeRatesSourceKind SourceKind { get; init; }
    public required Dictionary<string, decimal> RatesFromBase { get; init; }

    public bool HasRate(string code) => RatesFromBase.ContainsKey(code);

    public bool HasAllRates(IEnumerable<string> codes)
    {
        return codes.All(HasRate);
    }
}
