namespace CurrencyCalculator.Web.Core;

public sealed record CurrencyDefinition(
    string Code,
    CurrencyKind Kind,
    string NameEn,
    string NameZh);
