namespace CurrencyCalculator.Web.Models;

public sealed class CompareItemState
{
    public string CurrencyCode { get; set; } = "USD";
    public decimal Amount { get; set; } = 1m;
}
