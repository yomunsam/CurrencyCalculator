using Microsoft.JSInterop;

namespace CurrencyCalculator.Web.Services;

public sealed class BrowserContextService(IJSRuntime jsRuntime)
{
    public async Task<string> GetPreferredLanguageAsync()
    {
        var language = await jsRuntime.InvokeAsync<string?>("ccBrowser.getLanguage");
        return language ?? "en-US";
    }
}
