using Microsoft.JSInterop;

namespace CurrencyCalculator.Web.Services;

/// <summary>
/// Reads browser environment values (e.g. navigator.language) via JS interop.
/// </summary>
public sealed class BrowserContextService(IJSRuntime jsRuntime)
{
    public async Task<string> GetPreferredLanguageAsync()
    {
        var language = await jsRuntime.InvokeAsync<string?>("ccBrowser.getLanguage");
        return language ?? "en-US";
    }

    public async Task<string> GetTimezoneAsync()
    {
        var timezone = await jsRuntime.InvokeAsync<string?>("ccBrowser.getTimezone");
        return timezone ?? "";
    }
}
