using CurrencyCalculator.Web.Core;

namespace CurrencyCalculator.Web.Components.Currencies;

internal sealed record CurrencySelectionGroup(string Title, IReadOnlyList<CurrencyDefinition> Items);

internal static class CurrencySelection
{
    public static IReadOnlyList<CurrencySelectionGroup> GetGroups(
        IReadOnlyList<CurrencyDefinition> items,
        IReadOnlyList<string> recentCodes,
        string language,
        string query)
    {
        var search = query.Trim();
        if (search.Length > 0)
        {
            var matches = items
                .Where(item => item.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || item.GetName(language).Contains(search, StringComparison.OrdinalIgnoreCase)
                    || item.GetName("en-US").Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Code.Equals(search, StringComparison.OrdinalIgnoreCase))
                .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return [new("SearchResults", matches)];
        }

        var byCode = items.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);
        var recent = recentCodes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(byCode.ContainsKey)
            .Take(AppSettings.SelectorRecentMaxCount)
            .Select(code => byCode[code])
            .ToArray();
        var recentSet = recent.Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var remaining = items
            .Where(item => !recentSet.Contains(item.Code))
            .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return
        [
            new("RecentCurrencies", recent),
            new("FiatCurrencies", remaining.Where(item => item.Kind == CurrencyKind.Fiat).ToArray()),
            new("CryptoCurrencies", remaining.Where(item => item.Kind == CurrencyKind.Crypto).ToArray())
        ];
    }
}
