using System.Collections.ObjectModel;

namespace CurrencyCalculator.Web.Core;

public static class CurrencyCatalog
{
    // To add a new currency: add one entry here. Icon = flag emoji or crypto symbol.
    // Names dictionary keys must match LocalizationService.SupportedLanguages.
    private static readonly ReadOnlyCollection<CurrencyDefinition> ItemsInternal =
    [
        Fiat("USD", "🇺🇸", 2, "US Dollar", "美元"),
        Fiat("CNY", "🇨🇳", 2, "Chinese Yuan", "人民币"),
        Fiat("EUR", "🇪🇺", 2, "Euro", "欧元"),
        Fiat("JPY", "🇯🇵", 0, "Japanese Yen", "日元"),
        Fiat("GBP", "🇬🇧", 2, "British Pound", "英镑"),
        Fiat("HKD", "🇭🇰", 2, "Hong Kong Dollar", "港币"),
        Fiat("AUD", "🇦🇺", 2, "Australian Dollar", "澳元"),
        Fiat("CAD", "🇨🇦", 2, "Canadian Dollar", "加元"),
        Fiat("CHF", "🇨🇭", 2, "Swiss Franc", "瑞士法郎"),
        Fiat("NZD", "🇳🇿", 2, "New Zealand Dollar", "新西兰元"),
        Crypto("BTC", "₿", 8, "Bitcoin", "比特币"),
        Crypto("ETH", "Ξ", 8, "Ethereum", "以太坊")
    ];

    private static readonly Dictionary<string, CurrencyDefinition> ByCodeInternal =
        ItemsInternal.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<CurrencyDefinition> Items => ItemsInternal;

    public static IReadOnlyCollection<string> SupportedCodes { get; } =
        ItemsInternal.Select(item => item.Code).ToArray();

    public static IReadOnlyCollection<string> FiatCodes { get; } =
        ItemsInternal.Where(item => item.Kind == CurrencyKind.Fiat).Select(item => item.Code).ToArray();

    public static CurrencyDefinition GetByCode(string code)
    {
        if (ByCodeInternal.TryGetValue(code, out var item))
            return item;
        throw new InvalidOperationException($"Unsupported currency code: {code}");
    }

    public static bool IsSupported(string code) => ByCodeInternal.ContainsKey(code);

    // ── helpers to keep the table compact ──────────────────────────────
    private static CurrencyDefinition Fiat(string code, string icon, int decimals, string nameEn, string nameZh) =>
        new()
        {
            Code = code,
            Kind = CurrencyKind.Fiat,
            Icon = icon,
            DisplayDecimals = decimals,
            Names = new Dictionary<string, string> { ["en-US"] = nameEn, ["zh-CN"] = nameZh }
        };

    private static CurrencyDefinition Crypto(string code, string icon, int decimals, string nameEn, string nameZh) =>
        new()
        {
            Code = code,
            Kind = CurrencyKind.Crypto,
            Icon = icon,
            DisplayDecimals = decimals,
            Names = new Dictionary<string, string> { ["en-US"] = nameEn, ["zh-CN"] = nameZh }
        };
}
