using CurrencyCalculator.Web.Core;

namespace CurrencyCalculator.Web.Services;

public sealed class LocalizationService(
    BrowserContextService browserContextService,
    BrowserStorageService storageService)
{
    private static readonly Dictionary<string, Dictionary<string, string>> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppTitle"] = "Currency Calculator",
            ["AppSubtitle"] = "Compare fiat and crypto rates in real time with offline fallback",
            ["Live"] = "Live",
            ["Cache"] = "Local Cache",
            ["Fallback"] = "Static Fallback",
            ["AddItem"] = "Add comparison",
            ["Remove"] = "Remove",
            ["Refresh"] = "Refresh",
            ["Language"] = "Language",
            ["FocusedBaseHint"] = "Focused row is the base currency",
            ["UpdatedAt"] = "Updated",
            ["RateSource"] = "Source",
            ["Unavailable"] = "Rate unavailable",
            ["LoadFailed"] = "Unable to load rates from all sources."
        },
        ["zh-CN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppTitle"] = "汇率计算器",
            ["AppSubtitle"] = "支持法币与加密货币实时对比，离线可回退",
            ["Live"] = "实时",
            ["Cache"] = "本地缓存",
            ["Fallback"] = "静态回退",
            ["AddItem"] = "添加对比项",
            ["Remove"] = "删除",
            ["Refresh"] = "刷新",
            ["Language"] = "语言",
            ["FocusedBaseHint"] = "当前输入焦点行为基准货币",
            ["UpdatedAt"] = "更新时间",
            ["RateSource"] = "数据来源",
            ["Unavailable"] = "暂无汇率",
            ["LoadFailed"] = "所有数据源均无法获取汇率。"
        }
    };

    public static readonly IReadOnlyCollection<string> SupportedLanguages = ["en-US", "zh-CN"];

    public async Task<string> InitializeLanguageAsync()
    {
        var stored = await storageService.GetAsync<string>(AppSettings.LanguageStorageKey);
        if (IsSupportedLanguage(stored))
        {
            return Normalize(stored!);
        }

        var browser = await browserContextService.GetPreferredLanguageAsync();
        var normalized = Normalize(browser);
        await storageService.SetAsync(AppSettings.LanguageStorageKey, normalized);
        return normalized;
    }

    public Task SaveLanguageAsync(string language)
    {
        return storageService.SetAsync(AppSettings.LanguageStorageKey, Normalize(language));
    }

    public string T(string language, string key)
    {
        var normalized = Normalize(language);
        if (Texts.TryGetValue(normalized, out var map) && map.TryGetValue(key, out var text))
        {
            return text;
        }

        return Texts["en-US"].GetValueOrDefault(key, key);
    }

    public IReadOnlyList<string> GetDefaultCurrencyCodes(string language)
    {
        var normalized = Normalize(language);
        return normalized.Equals("zh-CN", StringComparison.OrdinalIgnoreCase)
            ? ["CNY", "USD"]
            : ["USD", "CNY"];
    }

    public static string Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en-US";
        }

        if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return "zh-CN";
        }

        return "en-US";
    }

    private static bool IsSupportedLanguage(string? language)
    {
        return !string.IsNullOrWhiteSpace(language) &&
               SupportedLanguages.Contains(Normalize(language), StringComparer.OrdinalIgnoreCase);
    }
}
