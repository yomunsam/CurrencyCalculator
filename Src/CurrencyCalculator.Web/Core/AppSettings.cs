namespace CurrencyCalculator.Web.Core;

public static class AppSettings
{
    public const int MinCompareItems = 2;
    public const int MaxCompareItems = 5;
    public const string PreferencesStorageKey = "cc.preferences.v1";
    public const string SelectorRecentStorageKey = "cc.selector-recent.v1";
    public const string LanguageStorageKey = "cc.language.v1";
    public const string RatesCacheStorageKey = "cc.rates-cache.v1";
    public const string ThemeStorageKey = "cc.theme.v1";
    public const int SelectorRecentMaxCount = 3;
    public const int RowDragLongPressMs = 300;
    public const int RowDragHorizontalCancelThresholdPx = 10;
    public static readonly TimeSpan RatesCacheTtl = TimeSpan.FromHours(24);
    public static readonly TimeSpan FiatStaleThreshold = TimeSpan.FromHours(24);
    public static readonly TimeSpan CryptoStaleThreshold = TimeSpan.FromHours(1);
}
