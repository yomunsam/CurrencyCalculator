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
            ["Clear"] = "Clear",
            ["Refresh"] = "Refresh",
            ["Language"] = "Language",
            ["FocusedBaseHint"] = "Tap a row to set it as the base currency",
            ["UpdatedAt"] = "Updated",
            ["RateSource"] = "Source",
            ["Unavailable"] = "Rate unavailable",
            ["LoadFailed"] = "Unable to load rates from all sources.",
            ["LoadingRates"] = "Preparing latest rates...",
            ["DuplicateCurrency"] = "Duplicate currency added – rates will be 1:1",
            ["NoMoreCurrencies"] = "All currencies are already in your comparison list.",
            ["StaleWarning"] = "Cached rates are stale and for reference only.",
            ["CryptoStaleWarning"] = "Crypto rate may be outdated (>1 h)",
            ["CryptoVolatileWarning"] = "Cryptocurrency rates are highly volatile. Use with caution.",
            ["AboutTitle"] = "About",
            ["AboutDesc"] = "A minimalist exchange rate tool supporting fiat and cryptocurrency, powered by free open APIs.",
            ["AboutDataSources"] = "Data Sources",
            ["AboutTechStack"] = "Tech Stack",
            ["AboutAIDisclosure"] = "AI Transparency",
            ["AboutAIDisclosureText"] = "AI tools were used to assist in code writing and documentation during development. All architecture decisions and key choices were made by human developers, and all code was fully reviewed by humans.",
            ["AboutDisclaimer"] = "Disclaimer",
            ["AboutDisclaimer1"] = "Exchange rate data is for reference only and may be delayed or inaccurate. Please verify before making important financial decisions.",
            ["AboutDisclaimer2"] = "This tool is open source. Community contributions and improvements are welcome.",
            ["AboutDisclaimer3"] = "Please review the code and assess security before use. The developer is not liable for any direct or indirect loss arising from the use of this tool, its code, or its data.",
            ["AboutDeveloper"] = "Developer",
            ["AboutGitHub"] = "GitHub Repository",
            ["Close"] = "Close",
            ["RefreshSuccess"] = "Rates updated",
            ["RefreshFailed"] = "Failed to update rates",
            ["Copyright"] = "Copyright © 2026 Yomu",
            ["Settings"] = "Settings",
            ["IgnoreFiatDecimals"] = "Ignore fiat decimal spec",
            ["IgnoreFiatDecimalsDesc"] = "Show up to 6 decimal places instead of ISO 4217 standard",
            ["HideCurrencyIcons"] = "Hide currency icons",
            ["HideCurrencyIconsDesc"] = "Do not display flag and symbol icons next to currency codes"
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
            ["Clear"] = "清空",
            ["Refresh"] = "刷新",
            ["Language"] = "语言",
            ["FocusedBaseHint"] = "点击某行将其设为基准货币",
            ["UpdatedAt"] = "更新时间",
            ["RateSource"] = "数据来源",
            ["Unavailable"] = "暂无汇率",
            ["LoadFailed"] = "所有数据源均无法获取汇率。",
            ["LoadingRates"] = "正在准备最新汇率数据…",
            ["DuplicateCurrency"] = "已添加重复货币，汇率将为 1:1",
            ["NoMoreCurrencies"] = "所有货币都已添加到对比列表中。",
            ["StaleWarning"] = "缓存的汇率数据已过期，仅供参考。",
            ["CryptoStaleWarning"] = "加密货币汇率可能已过时（>1小时）",
            ["CryptoVolatileWarning"] = "加密货币汇率波动较大，请谨慎参考。",
            ["AboutTitle"] = "关于",
            ["AboutDesc"] = "极简的汇率换算工具，支持法币和加密货币，数据来源于公开的免费 API。",
            ["AboutDataSources"] = "数据来源",
            ["AboutTechStack"] = "技术栈",
            ["AboutAIDisclosure"] = "AI 透明度揭示",
            ["AboutAIDisclosureText"] = "本项目的开发过程中，AI 工具被用来辅助代码编写和文档撰写。所有架构设计和关键决策由人类开发者完成，并完全由人类进行了 Review。",
            ["AboutDisclaimer"] = "免责声明",
            ["AboutDisclaimer1"] = "本工具提供的汇率数据仅供参考，可能存在延迟或不准确的情况。请在进行重要的财务决策前，务必核实数据的准确性。",
            ["AboutDisclaimer2"] = "本工具开源，欢迎社区参与改进和优化。",
            ["AboutDisclaimer3"] = "使用前请自行审阅代码并评估安全性，开发者不对任何因使用本工具、使用代码、使用本工具得到的数据而产生的直接或间接损失负责。",
            ["AboutDeveloper"] = "开发者",
            ["AboutGitHub"] = "GitHub 仓库",
            ["Close"] = "关闭",
            ["RefreshSuccess"] = "汇率已更新",
            ["RefreshFailed"] = "更新汇率失败",
            ["Copyright"] = "Copyright © 2026 Yomu",
            ["Settings"] = "设置",
            ["IgnoreFiatDecimals"] = "忽略法币小数位规范",
            ["IgnoreFiatDecimalsDesc"] = "显示最多6位小数，而非 ISO 4217 标准",
            ["HideCurrencyIcons"] = "隐藏货币图标",
            ["HideCurrencyIconsDesc"] = "不在货币代码旁显示国旗和标识图标"
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
            ["Clear"] = "クリア",
            ["Refresh"] = "更新",
            ["Language"] = "言語",
            ["FocusedBaseHint"] = "行をタップして基準通貨に設定",
            ["UpdatedAt"] = "更新日時",
            ["RateSource"] = "データソース",
            ["Unavailable"] = "レートなし",
            ["LoadFailed"] = "すべてのデータソースからレートを取得できませんでした。",
            ["LoadingRates"] = "最新レートを準備中です…",
            ["DuplicateCurrency"] = "重複した通貨が追加されました — レートは1:1になります",
            ["NoMoreCurrencies"] = "比較リストに追加できる通貨はもうありません。",
            ["StaleWarning"] = "キャッシュされたレートは古くなっています。参考値です。",
            ["CryptoStaleWarning"] = "暗号資産レートが古い可能性があります（>1時間）",
            ["CryptoVolatileWarning"] = "暗号資産のレートは変動が大きいため、参考程度にご利用ください。",
            ["AboutTitle"] = "このアプリについて",
            ["AboutDesc"] = "法定通貨と暗号資産に対応したミニマルな為替計算ツール。無料の公開 API からデータを取得しています。",
            ["AboutDataSources"] = "データソース",
            ["AboutTechStack"] = "技術スタック",
            ["AboutAIDisclosure"] = "AI 透明性の開示",
            ["AboutAIDisclosureText"] = "本プロジェクトの開発では、AI ツールがコード記述とドキュメント作成の補助に使用されました。すべてのアーキテクチャ設計と主要な意思決定は人間の開発者が行い、すべてのコードは人間によって完全にレビューされています。",
            ["AboutDisclaimer"] = "免責事項",
            ["AboutDisclaimer1"] = "本ツールが提供する為替レートは参考値であり、遅延や不正確な場合があります。重要な財務上の判断を行う前に、データの正確性を必ず確認してください。",
            ["AboutDisclaimer2"] = "本ツールはオープンソースです。コミュニティからの改善への参加を歓迎します。",
            ["AboutDisclaimer3"] = "使用前にコードを確認しセキュリティを評価してください。開発者は、本ツール・コード・データの使用により生じたいかなる直接的・間接的損失についても責任を負いません。",
            ["AboutDeveloper"] = "開発者",
            ["AboutGitHub"] = "GitHub リポジトリ",
            ["Close"] = "閉じる",
            ["RefreshSuccess"] = "レートを更新しました",
            ["RefreshFailed"] = "レートの更新に失敗しました",
            ["Copyright"] = "Copyright © 2026 Yomu",
            ["Settings"] = "設定",
            ["IgnoreFiatDecimals"] = "法定通貨の小数桁仕様を無視",
            ["IgnoreFiatDecimalsDesc"] = "ISO 4217 標準ではなく、最大6桁の小数を表示",
            ["HideCurrencyIcons"] = "通貨アイコンを非表示",
            ["HideCurrencyIconsDesc"] = "通貨コードの横に国旗やシンボルアイコンを表示しない"
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
