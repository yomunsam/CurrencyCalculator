using CurrencyCalculator.Web.Core;

namespace CurrencyCalculator.Web.Services;

/// <summary>
/// Data-driven i18n service.
/// To add a new language: add an entry to <see cref="Languages"/> and its texts to <see cref="Texts"/>.
/// </summary>
public sealed class LocalizationService(
    BrowserContextService browserContextService,
    BrowserStorageService storageService)
{
    // ── Supported languages ────────────────────────────────────────────
    // Each entry: code → display label, default currency pair
    public static readonly IReadOnlyList<LanguageEntry> Languages =
    [
        new("en-US", "English", ["USD", "CNY"]),
        new("zh-CN", "简体中文", ["CNY", "USD"]),
        new("ja-JP", "日本語", ["JPY", "USD"])
    ];

    public static IReadOnlyCollection<string> SupportedLanguages { get; } =
        Languages.Select(l => l.Code).ToArray();

    // ── Translation table ──────────────────────────────────────────────
    private static readonly Dictionary<string, Dictionary<string, string>> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppTitle"] = "Currency Calculator",
            ["AppSubtitle"] = "Real-time multi-currency comparison with offline fallback",
            ["Live"] = "Live",
            ["Cache"] = "Cache",
            ["Fallback"] = "Fallback",
            ["AddItem"] = "Add",
            ["Remove"] = "Remove",
            ["Refresh"] = "Refresh",
            ["Language"] = "Language",
            ["FocusedBaseHint"] = "Tap a row to set it as the base currency",
            ["UpdatedAt"] = "Updated",
            ["RateSource"] = "Source",
            ["Unavailable"] = "Rate unavailable",
            ["LoadFailed"] = "Unable to load rates from all sources.",
            ["DuplicateCurrency"] = "Duplicate currency added – rates will be 1:1",
            ["StaleWarning"] = "Cached rates are stale and for reference only.",
            ["CryptoStaleWarning"] = "Crypto rate may be outdated (>1 h)",
            ["AboutTitle"] = "About",
            ["AboutDescription"] = "A lightweight PWA currency calculator built with Blazor WebAssembly. Rates from fawazahmed0/exchange-api & Frankfurter.",
            ["AboutDeveloper"] = "Developer",
            ["AboutGitHub"] = "GitHub Repository",
            ["Close"] = "Close",
            ["RefreshSuccess"] = "Rates updated",
            ["RefreshFailed"] = "Failed to update rates",
            ["Copyright"] = "Copyright © 2026 Yomu"
        },
        ["zh-CN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppTitle"] = "汇率计算器",
            ["AppSubtitle"] = "多币种实时对比，支持离线回退",
            ["Live"] = "实时",
            ["Cache"] = "缓存",
            ["Fallback"] = "回退",
            ["AddItem"] = "添加",
            ["Remove"] = "删除",
            ["Refresh"] = "刷新",
            ["Language"] = "语言",
            ["FocusedBaseHint"] = "点击某行将其设为基准货币",
            ["UpdatedAt"] = "更新时间",
            ["RateSource"] = "数据来源",
            ["Unavailable"] = "暂无汇率",
            ["LoadFailed"] = "所有数据源均无法获取汇率。",
            ["DuplicateCurrency"] = "已添加重复货币，汇率将为 1:1",
            ["StaleWarning"] = "缓存的汇率数据已过期，仅供参考。",
            ["CryptoStaleWarning"] = "加密货币汇率可能已过时（>1小时）",
            ["AboutTitle"] = "关于",
            ["AboutDescription"] = "基于 Blazor WebAssembly 的轻量级 PWA 汇率计算器。数据来自 fawazahmed0/exchange-api 与 Frankfurter。",
            ["AboutDeveloper"] = "开发者",
            ["AboutGitHub"] = "GitHub 仓库",
            ["Close"] = "关闭",
            ["RefreshSuccess"] = "汇率已更新",
            ["RefreshFailed"] = "更新汇率失败",
            ["Copyright"] = "Copyright © 2026 Yomu"
        },
        ["ja-JP"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppTitle"] = "為替計算機",
            ["AppSubtitle"] = "リアルタイム多通貨比較・オフラインフォールバック対応",
            ["Live"] = "リアルタイム",
            ["Cache"] = "キャッシュ",
            ["Fallback"] = "フォールバック",
            ["AddItem"] = "追加",
            ["Remove"] = "削除",
            ["Refresh"] = "更新",
            ["Language"] = "言語",
            ["FocusedBaseHint"] = "行をタップして基準通貨に設定",
            ["UpdatedAt"] = "更新日時",
            ["RateSource"] = "データソース",
            ["Unavailable"] = "レートなし",
            ["LoadFailed"] = "すべてのデータソースからレートを取得できませんでした。",
            ["DuplicateCurrency"] = "重複した通貨が追加されました — レートは1:1になります",
            ["StaleWarning"] = "キャッシュされたレートは古くなっています。参考値です。",
            ["CryptoStaleWarning"] = "暗号資産レートが古い可能性があります（>1時間）",
            ["AboutTitle"] = "このアプリについて",
            ["AboutDescription"] = "Blazor WebAssembly で構築された軽量PWA為替計算機。レートは fawazahmed0/exchange-api と Frankfurter より取得。",
            ["AboutDeveloper"] = "開発者",
            ["AboutGitHub"] = "GitHub リポジトリ",
            ["Close"] = "閉じる",
            ["RefreshSuccess"] = "レートを更新しました",
            ["RefreshFailed"] = "レートの更新に失敗しました",
            ["Copyright"] = "Copyright © 2026 Yomu"
        }
    };

    // ── Public API ─────────────────────────────────────────────────────
    public async Task<string> InitializeLanguageAsync()
    {
        var stored = await storageService.GetAsync<string>(AppSettings.LanguageStorageKey);
        if (IsSupportedLanguage(stored))
            return Normalize(stored!);

        var browser = await browserContextService.GetPreferredLanguageAsync();
        var normalized = Normalize(browser);
        await storageService.SetAsync(AppSettings.LanguageStorageKey, normalized);
        return normalized;
    }

    public Task SaveLanguageAsync(string language) =>
        storageService.SetAsync(AppSettings.LanguageStorageKey, Normalize(language));

    public string T(string language, string key)
    {
        var normalized = Normalize(language);
        if (Texts.TryGetValue(normalized, out var map) && map.TryGetValue(key, out var text))
            return text;
        return Texts["en-US"].GetValueOrDefault(key, key);
    }

    public IReadOnlyList<string> GetDefaultCurrencyCodes(string language)
    {
        var normalized = Normalize(language);
        var entry = Languages.FirstOrDefault(l => l.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        return entry?.DefaultCurrencies ?? ["USD", "CNY"];
    }

    public static string Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "en-US";
        // Try exact match first
        var exact = Languages.FirstOrDefault(l => l.Code.Equals(language, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact.Code;
        // Try prefix match (e.g. "zh" → "zh-CN")
        var prefix = Languages.FirstOrDefault(l => language.StartsWith(l.Code[..2], StringComparison.OrdinalIgnoreCase));
        return prefix?.Code ?? "en-US";
    }

    public static LanguageEntry? GetLanguageEntry(string code) =>
        Languages.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    private static bool IsSupportedLanguage(string? language) =>
        !string.IsNullOrWhiteSpace(language) &&
        SupportedLanguages.Contains(Normalize(language), StringComparer.OrdinalIgnoreCase);
}

public sealed record LanguageEntry(string Code, string DisplayName, IReadOnlyList<string> DefaultCurrencies);
