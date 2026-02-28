namespace CurrencyCalculator.Web.Core;

public static class AppSettings
{
    public const int MinCompareItems = 2;
    public const int MaxCompareItems = 5;
    public const string PreferencesStorageKey = "cc.preferences.v1";
    public const string LanguageStorageKey = "cc.language.v1";
    public const string RatesCacheStorageKey = "cc.rates-cache.v1";
    public static readonly TimeSpan RatesCacheTtl = TimeSpan.FromHours(24);
}
