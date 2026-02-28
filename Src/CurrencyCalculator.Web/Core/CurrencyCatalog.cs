using System.Collections.ObjectModel;

namespace CurrencyCalculator.Web.Core;

public static class CurrencyCatalog
{
    private static readonly ReadOnlyCollection<CurrencyDefinition> ItemsInternal =
    [
        new("USD", CurrencyKind.Fiat, "US Dollar", "美元"),
        new("CNY", CurrencyKind.Fiat, "Chinese Yuan", "人民币"),
        new("EUR", CurrencyKind.Fiat, "Euro", "欧元"),
        new("JPY", CurrencyKind.Fiat, "Japanese Yen", "日元"),
        new("GBP", CurrencyKind.Fiat, "British Pound", "英镑"),
        new("HKD", CurrencyKind.Fiat, "Hong Kong Dollar", "港币"),
        new("AUD", CurrencyKind.Fiat, "Australian Dollar", "澳元"),
        new("CAD", CurrencyKind.Fiat, "Canadian Dollar", "加元"),
        new("CHF", CurrencyKind.Fiat, "Swiss Franc", "瑞士法郎"),
        new("NZD", CurrencyKind.Fiat, "New Zealand Dollar", "新西兰元"),
        new("BTC", CurrencyKind.Crypto, "Bitcoin", "比特币"),
        new("ETH", CurrencyKind.Crypto, "Ethereum", "以太坊")
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
        {
            return item;
        }

        throw new InvalidOperationException($"Unsupported currency code: {code}");
    }

    public static bool IsSupported(string code)
    {
        return ByCodeInternal.ContainsKey(code);
    }
}
