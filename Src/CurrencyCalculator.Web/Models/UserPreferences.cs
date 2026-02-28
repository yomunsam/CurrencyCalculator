namespace CurrencyCalculator.Web.Models;

public sealed class UserPreferences
{
    public string Language { get; set; } = "en-US";
    public int ActiveIndex { get; set; }
    public List<CompareItemState> Items { get; set; } = [];
}
