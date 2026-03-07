using System.Text.Json.Serialization;
using CurrencyCalculator.Web.Models;

namespace CurrencyCalculator.Web;

/// <summary>
/// Source-generated JSON serializer context.
/// Registers every model type that the app serializes/deserializes so that the
/// .NET IL trimmer can remove all reflection-based JSON paths, keeping the
/// published output as small as possible.
/// </summary>
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(ExchangeRatesSnapshot))]
[JsonSerializable(typeof(UserPreferences))]
[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
internal partial class AppJsonContext : JsonSerializerContext { }
