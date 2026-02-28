namespace CurrencyCalculator.Web.Core;

public sealed record CurrencyDefinition
{
    public required string Code { get; init; }
    public required CurrencyKind Kind { get; init; }
    /// <summary>Flag emoji for fiat (🇺🇸) or symbol for crypto (₿).</summary>
    public required string Icon { get; init; }
    /// <summary>Decimal places used for display formatting.</summary>
    public int DisplayDecimals { get; init; } = 2;
    /// <summary>Localized display names keyed by language code (e.g. "en-US", "zh-CN").</summary>
    public required Dictionary<string, string> Names { get; init; }

    public string GetName(string language)
    {
        if (Names.TryGetValue(language, out var name))
            return name;
        if (Names.TryGetValue("en-US", out var fallback))
            return fallback;
        return Code;
    }
}
