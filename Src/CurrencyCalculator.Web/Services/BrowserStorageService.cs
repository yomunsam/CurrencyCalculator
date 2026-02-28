using System.Text.Json;
using Microsoft.JSInterop;

namespace CurrencyCalculator.Web.Services;

public sealed class BrowserStorageService(IJSRuntime jsRuntime)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ValueTask RemoveAsync(string key)
    {
        return jsRuntime.InvokeVoidAsync("ccStorage.remove", key);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await jsRuntime.InvokeAsync<string?>("ccStorage.get", key);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await jsRuntime.InvokeVoidAsync("ccStorage.set", key, json);
    }
}
