using System.Collections.ObjectModel;

namespace CurrencyCalculator.Web.Core;

/// <summary>
/// Central registry of supported currencies.
/// To add a new fiat currency: add one <c>Fiat(...)</c> line below.
/// To add a new cryptocurrency: add one <c>Crypto(...)</c> line.
/// Icon = 2-letter country code for fiat (used with flagcdn.com) or symbol for crypto.
/// </summary>
public static class CurrencyCatalog
{
    // To add a new currency: add one entry here. Icon = 2-letter country code (fiat) or crypto symbol.
    // Names dictionary keys must match LocalizationService.SupportedLanguages.
    private static readonly ReadOnlyCollection<CurrencyDefinition> ItemsInternal =
    [
        Fiat("USD", "us", 2, "US Dollar", "美元", "米ドル"),
        Fiat("CNY", "cn", 2, "Chinese Yuan", "人民币", "人民元"),
        Fiat("EUR", "eu", 2, "Euro", "欧元", "ユーロ"),
        Fiat("JPY", "jp", 0, "Japanese Yen", "日元", "日本円"),
        Fiat("GBP", "gb", 2, "British Pound", "英镑", "英ポンド"),
        Fiat("HKD", "hk", 2, "Hong Kong Dollar", "港币", "香港ドル"),
        Fiat("AUD", "au", 2, "Australian Dollar", "澳元", "豪ドル"),
        Fiat("CAD", "ca", 2, "Canadian Dollar", "加元", "カナダドル"),
        Fiat("CHF", "ch", 2, "Swiss Franc", "瑞士法郎", "スイスフラン"),
        Fiat("NZD", "nz", 2, "New Zealand Dollar", "新西兰元", "NZドル"),
        Fiat("TWD", "tw", 0, "New Taiwan Dollar", "新台币", "台湾ドル"),
        Crypto("BTC", "₿", 8, "Bitcoin", "比特币", "ビットコイン"),
        Crypto("ETH", "Ξ", 8, "Ethereum", "以太坊", "イーサリアム")
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
    private static CurrencyDefinition Fiat(string code, string icon, int decimals, string nameEn, string nameZh, string nameJa) =>
        new()
        {
            Code = code,
            Kind = CurrencyKind.Fiat,
            Icon = icon,
            DisplayDecimals = decimals,
            Names = new Dictionary<string, string> { ["en-US"] = nameEn, ["zh-CN"] = nameZh, ["ja-JP"] = nameJa }
        };

    private static CurrencyDefinition Crypto(string code, string icon, int decimals, string nameEn, string nameZh, string nameJa) =>
        new()
        {
            Code = code,
            Kind = CurrencyKind.Crypto,
            Icon = icon,
            DisplayDecimals = decimals,
            Names = new Dictionary<string, string> { ["en-US"] = nameEn, ["zh-CN"] = nameZh, ["ja-JP"] = nameJa }
        };
}
