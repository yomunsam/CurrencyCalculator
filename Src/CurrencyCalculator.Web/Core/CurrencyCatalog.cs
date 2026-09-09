using System.Collections.ObjectModel;

namespace CurrencyCalculator.Web.Core;

/// <summary>
/// 支持币种的统一目录；新增币种还需同步本地图标与静态汇率。
/// </summary>
public static class CurrencyCatalog
{
    // 法币图标为本地 SVG 的国家或地区代码，名称覆盖当前支持的三种语言。
    private static readonly ReadOnlyCollection<CurrencyDefinition> ItemsInternal =
    [
        Fiat("USD", "us", 2, "US Dollar", "美元", "米ドル"),
        Fiat("CNY", "cn", 2, "Chinese Yuan", "人民币", "人民元"),
        Fiat("EUR", "eu", 2, "Euro", "欧元", "ユーロ"),
        Fiat("JPY", "jp", 0, "Japanese Yen", "日元", "日本円"),
        Fiat("GBP", "gb", 2, "British Pound", "英镑", "英ポンド"),
        Fiat("HKD", "hk", 2, "Hong Kong Dollar", "港币", "香港ドル"),
        Fiat("MOP", "mo", 2, "Macanese Pataca", "澳门元", "マカオ・パタカ"),
        Fiat("SGD", "sg", 2, "Singapore Dollar", "新加坡元", "シンガポールドル"),
        Fiat("AUD", "au", 2, "Australian Dollar", "澳元", "豪ドル"),
        Fiat("CAD", "ca", 2, "Canadian Dollar", "加元", "カナダドル"),
        Fiat("CHF", "ch", 2, "Swiss Franc", "瑞士法郎", "スイスフラン"),
        Fiat("NZD", "nz", 2, "New Zealand Dollar", "新西兰元", "NZドル"),
        Fiat("TWD", "tw", 0, "New Taiwan Dollar", "新台币", "台湾ドル"),
        Fiat("PHP", "ph", 2, "Philippine Peso", "菲律宾比索", "フィリピン・ペソ"),
        Fiat("KRW", "kr", 0, "South Korean Won", "韩元", "韓国ウォン"),
        Crypto("BTC", "btc", 8, "Bitcoin", "比特币", "ビットコイン"),
        Crypto("ETH", "eth", 8, "Ethereum", "以太坊", "イーサリアム"),
        Crypto("SOL", "sol", 8, "Solana", "索拉纳", "ソラナ")
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

    // 集中构造定义，便于直接阅读上方币种表。
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
