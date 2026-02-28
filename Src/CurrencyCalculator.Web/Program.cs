using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CurrencyCalculator.Web;
using CurrencyCalculator.Web.Services;
using CurrencyCalculator.Web.Services.Rates;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<BrowserStorageService>();
builder.Services.AddScoped<BrowserContextService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddScoped<IExchangeRateProvider, FawazExchangeApiProvider>();
builder.Services.AddScoped<IExchangeRateProvider, FrankfurterExchangeRateProvider>();
builder.Services.AddScoped<IExchangeRateProvider, StaticFallbackRateProvider>();

await builder.Build().RunAsync();
