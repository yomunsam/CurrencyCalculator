namespace CurrencyCalculator.Web.Core;

public static class AppSettings
{
    public const int MinCompareItems = 2;
    public const int MaxCompareItems = 8;
    public const string PreferencesStorageKey = "cc.preferences.v1";
    public const string SelectorRecentStorageKey = "cc.selector-recent.v1";
    public const string LanguageStorageKey = "cc.language.v1";
    public const string RatesCacheStorageKey = "cc.rates-cache.v1";
    public const string ThemeStorageKey = "cc.theme.v1";
    public const string IgnoreFiatDecimalsStorageKey = "cc.ignore-fiat-decimals.v1";
    public const string HideCurrencyIconsStorageKey = "cc.hide-currency-icons.v1";
    public const string NeverUseKeyboardStorageKey = "cc.never-use-keyboard.v1";
    public const int SelectorRecentMaxCount = 3;
    public static readonly TimeSpan OnlineProviderTimeout = TimeSpan.FromSeconds(4);
    public static readonly TimeSpan LocalProviderTimeout = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan RatesCacheTtl = TimeSpan.FromHours(24);
    public static readonly TimeSpan FiatStaleThreshold = TimeSpan.FromHours(24);
    public static readonly TimeSpan CryptoStaleThreshold = TimeSpan.FromHours(1);
}
