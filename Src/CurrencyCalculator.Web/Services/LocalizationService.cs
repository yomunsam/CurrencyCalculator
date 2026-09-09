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
    // 每项包含语言代码、显示名称和首次使用的默认币种。
    public static readonly IReadOnlyList<LanguageEntry> Languages =
    [
        new("en-US", "English", ["USD", "EUR", "ETH"]),
        new("zh-CN", "简体中文", ["USD", "HKD", "ETH"]),
        new("ja-JP", "日本語", ["JPY", "USD", "ETH"])
    ];

    public static IReadOnlyCollection<string> SupportedLanguages { get; } =
        Languages.Select(l => l.Code).ToArray();

    // ── Translation table ──────────────────────────────────────────────
    private static readonly Dictionary<string, Dictionary<string, string>> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["OfflineAndUpdates"] = "Offline use & updates",
            ["PwaPreparing"] = "Preparing offline files… Keep this page open until ready.",
            ["PwaReady"] = "Offline files are ready. Saved rates may be outdated; check their timestamp.",
            ["PwaUpdateReady"] = "An update is ready. Close all tabs and installed app windows for this site, then reopen to use it.",
            ["PwaDevelopment"] = "Development preview does not cache offline files. Test offline use with a published build.",
            ["PwaUnsupported"] = "Offline installation requires HTTPS (or localhost) and a browser with Service Worker support.",
            ["PwaFailed"] = "Offline files could not be prepared. Reconnect and reopen the page to retry.",
            ["PwaUnknown"] = "Offline readiness could not be confirmed. Keep a connection until an update is ready.",
            ["StorageUnavailable"] = "Browser storage is unavailable or full. Changes work in this session but may not survive reopening.",
            ["RefreshUsingSaved"] = "Live rates unavailable; keeping saved rates.",
            ["ToggleTheme"] = "Switch theme",
            ["CalculatorKeyboard"] = "Calculator",
            ["SystemKeyboard"] = "System keyboard",
            ["Backspace"] = "Backspace",
            ["Calculate"] = "Calculate",
            ["InvalidExpression"] = "Check the expression (including division by zero).",
            ["NeverUseKeyboard"] = "Never use built-in keyboard",
            ["NeverUseKeyboardDesc"] = "Always use your system keyboard, including touch and pen input.",
            ["ShowCurrencyIcons"] = "Show currency icons",
            ["ShowCurrencyIconsDesc"] = "Flags and symbols beside currency codes.",
            ["IconWarningTitle"] = "Currency icons",
            ["IconWarningText"] = "Third-party flags and symbols are for identification only and do not represent the developer’s views. Some may cause discomfort due to cultural or personal differences; the developer is not responsible for related disputes. Icons are hidden by default for your time zone. Enable them only if you are comfortable; you can hide them again in Settings.",
            ["KeepIconsHidden"] = "Keep hidden",
            ["ConfirmShowIcons"] = "Enable icons",
            ["AppTitle"] = "Currency Calculator",
            ["Live"] = "Live rates",
            ["Cache"] = "Locally cached rates",
            ["Fallback"] = "Daily rates",
            ["SelectCurrency"] = "Select currency",
            ["SearchCurrencies"] = "Search code or currency name",
            ["NoCurrencyResults"] = "No matching currencies",
            ["SearchResults"] = "Search results",
            ["RecentCurrencies"] = "Recently used",
            ["FiatCurrencies"] = "Currencies",
            ["CryptoCurrencies"] = "Cryptocurrencies",
            ["RowActions"] = "Row actions; drag to reorder",
            ["MoveUp"] = "Move up",
            ["MoveDown"] = "Move down",
            ["CopyValue"] = "Copy value",
            ["CopySuccess"] = "Value copied",
            ["CopyFailed"] = "Unable to copy. Please try again.",
            ["Amount"] = "Amount",
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
            ["OfflineAndUpdates"] = "离线与更新",
            ["PwaPreparing"] = "正在准备离线文件，请保持页面打开直到完成。",
            ["PwaReady"] = "离线文件已就绪。离线汇率可能过期，请留意更新时间。",
            ["PwaUpdateReady"] = "新版本已准备好。关闭本站所有标签页和已安装应用窗口，再重新打开即可更新。",
            ["PwaDevelopment"] = "开发预览不缓存离线文件，离线功能需使用发布版验证。",
            ["PwaUnsupported"] = "离线安装需要 HTTPS（或 localhost）和支持 Service Worker 的浏览器。",
            ["PwaFailed"] = "离线文件准备失败，请联网后重新打开页面重试。",
            ["PwaUnknown"] = "暂时无法确认离线状态，请保持联网，等待更新就绪。",
            ["StorageUnavailable"] = "浏览器存储不可用或已满。本次修改仍可使用，但重新打开后可能无法保留。",
            ["RefreshUsingSaved"] = "未能获取实时汇率，继续使用已有汇率。",
            ["ToggleTheme"] = "切换主题",
            ["CalculatorKeyboard"] = "计算键盘",
            ["SystemKeyboard"] = "系统键盘",
            ["Backspace"] = "退格",
            ["Calculate"] = "计算",
            ["InvalidExpression"] = "请检查算式是否完整，以及是否除以零。",
            ["NeverUseKeyboard"] = "永不使用内置屏幕键盘",
            ["NeverUseKeyboardDesc"] = "触摸和手写笔输入也使用系统键盘。",
            ["ShowCurrencyIcons"] = "显示货币图标",
            ["ShowCurrencyIconsDesc"] = "在货币代码旁显示旗帜或符号。",
            ["IconWarningTitle"] = "货币图标使用声明",
            ["IconWarningText"] = "第三方旗帜和符号仅用于识别货币，不代表开发者的立场。部分标识可能因文化或个人差异引起不适，开发者不对相关不适或争议承担责任。当前时区默认隐藏图标；请在了解后自愿开启，也可随时在设置中关闭。",
            ["KeepIconsHidden"] = "保持关闭",
            ["ConfirmShowIcons"] = "了解并开启",
            ["AppTitle"] = "汇率计算器",
            ["Live"] = "实时汇率",
            ["Cache"] = "本地缓存汇率",
            ["Fallback"] = "当日汇率",
            ["SelectCurrency"] = "选择货币",
            ["SearchCurrencies"] = "搜索货币代码或名称",
            ["NoCurrencyResults"] = "没有找到匹配的货币",
            ["SearchResults"] = "搜索结果",
            ["RecentCurrencies"] = "最近使用",
            ["FiatCurrencies"] = "法定货币",
            ["CryptoCurrencies"] = "加密货币",
            ["RowActions"] = "行操作；拖动可排序",
            ["MoveUp"] = "上移",
            ["MoveDown"] = "下移",
            ["CopyValue"] = "复制值",
            ["CopySuccess"] = "数值已复制",
            ["CopyFailed"] = "复制失败，请重试",
            ["Amount"] = "金额",
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
            ["OfflineAndUpdates"] = "オフラインと更新",
            ["PwaPreparing"] = "オフライン用ファイルを準備中です。完了までページを開いたままにしてください。",
            ["PwaReady"] = "オフライン用ファイルは準備済みです。保存レートの更新日時を確認してください。",
            ["PwaUpdateReady"] = "更新の準備ができました。このサイトの全タブとインストール済みアプリを閉じ、開き直してください。",
            ["PwaDevelopment"] = "開発プレビューはオフラインキャッシュを作成しません。公開用ビルドで確認してください。",
            ["PwaUnsupported"] = "オフライン機能には HTTPS（または localhost）と Service Worker 対応ブラウザーが必要です。",
            ["PwaFailed"] = "オフライン用ファイルを準備できませんでした。接続後にページを開き直してください。",
            ["PwaUnknown"] = "オフラインの準備状況を確認できません。更新の準備ができるまで接続を維持してください。",
            ["StorageUnavailable"] = "ブラウザーの保存領域が使えないか満杯です。変更は現在のセッションのみ有効な場合があります。",
            ["RefreshUsingSaved"] = "最新レートを取得できないため、保存済みレートを使用します。",
            ["ToggleTheme"] = "テーマを切り替え",
            ["CalculatorKeyboard"] = "計算キーボード",
            ["SystemKeyboard"] = "標準キーボード",
            ["Backspace"] = "一文字削除",
            ["Calculate"] = "計算",
            ["InvalidExpression"] = "式が未完成、またはゼロ除算になっていないか確認してください。",
            ["NeverUseKeyboard"] = "内蔵キーボードを使用しない",
            ["NeverUseKeyboardDesc"] = "タッチ・ペン入力でも標準キーボードを使用します。",
            ["ShowCurrencyIcons"] = "通貨アイコンを表示",
            ["ShowCurrencyIconsDesc"] = "通貨コードの横に旗や記号を表示します。",
            ["IconWarningTitle"] = "通貨アイコンについて",
            ["IconWarningText"] = "第三者の旗や記号は通貨の識別用であり、開発者の立場を示すものではありません。文化や個人の違いによる不快感・争議について開発者は責任を負いません。現在のタイムゾーンでは初期設定で非表示です。了承した場合のみ有効にしてください。設定で再び非表示にできます。",
            ["KeepIconsHidden"] = "非表示のまま",
            ["ConfirmShowIcons"] = "了承して表示",
            ["AppTitle"] = "為替計算機",
            ["Live"] = "リアルタイムレート",
            ["Cache"] = "ローカルキャッシュレート",
            ["Fallback"] = "当日レート",
            ["SelectCurrency"] = "通貨を選択",
            ["SearchCurrencies"] = "通貨コードまたは名前で検索",
            ["NoCurrencyResults"] = "一致する通貨がありません",
            ["SearchResults"] = "検索結果",
            ["RecentCurrencies"] = "最近使用した通貨",
            ["FiatCurrencies"] = "法定通貨",
            ["CryptoCurrencies"] = "暗号資産",
            ["RowActions"] = "行の操作・ドラッグで並べ替え",
            ["MoveUp"] = "上へ移動",
            ["MoveDown"] = "下へ移動",
            ["CopyValue"] = "数値をコピー",
            ["CopySuccess"] = "数値をコピーしました",
            ["CopyFailed"] = "コピーできませんでした。もう一度お試しください。",
            ["Amount"] = "金額",
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
        return entry?.DefaultCurrencies ?? ["USD", "EUR", "ETH"];
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
