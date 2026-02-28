using System.Globalization;
using System.Text.RegularExpressions;

namespace CurrencyCalculator.Web.Core;

/// <summary>
/// Evaluates simple arithmetic expressions: +, -, *, / with decimal operands.
/// Supports grouping with parentheses. No external dependencies.
/// </summary>
public static partial class MathExpressionEvaluator
{
    private static readonly Regex TokenPattern = BuildTokenPattern();

    /// <summary>
    /// Try to evaluate an expression string (e.g. "100*1.2+50") and return the result.
    /// Returns false if the input is not a valid expression.
    /// </summary>
    public static bool TryEvaluate(string? input, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Strip thousand-separators (commas) and whitespace
        var cleaned = input.Replace(",", "").Replace(" ", "").Replace("_", "");

        // Fast path: plain number
        if (decimal.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            return true;

        // Must contain at least one operator to be an expression
        if (!cleaned.AsSpan().ContainsAny("+-*/"))
            return false;

        try
        {
            var tokens = Tokenize(cleaned);
            result = Parse(tokens, 0, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<string> Tokenize(string expr)
    {
        var tokens = new List<string>();
        foreach (Match match in TokenPattern.Matches(expr))
            tokens.Add(match.Value);
        return tokens;
    }

    // Recursive descent parser: expr = term ((+|-) term)*
    private static decimal Parse(List<string> tokens, int pos, out int nextPos)
    {
        var left = ParseTerm(tokens, pos, out nextPos);
        while (nextPos < tokens.Count && tokens[nextPos] is "+" or "-")
        {
            var op = tokens[nextPos];
            var right = ParseTerm(tokens, nextPos + 1, out nextPos);
            left = op == "+" ? left + right : left - right;
        }
        return left;
    }

    // term = factor ((*|/) factor)*
    private static decimal ParseTerm(List<string> tokens, int pos, out int nextPos)
    {
        var left = ParseFactor(tokens, pos, out nextPos);
        while (nextPos < tokens.Count && tokens[nextPos] is "*" or "/")
        {
            var op = tokens[nextPos];
            var right = ParseFactor(tokens, nextPos + 1, out nextPos);
            left = op == "*" ? left * right : right != 0 ? left / right : 0;
        }
        return left;
    }

    // factor = number | '(' expr ')'
    private static decimal ParseFactor(List<string> tokens, int pos, out int nextPos)
    {
        if (pos >= tokens.Count)
        {
            nextPos = pos;
            return 0;
        }

        if (tokens[pos] == "(")
        {
            var value = Parse(tokens, pos + 1, out nextPos);
            if (nextPos < tokens.Count && tokens[nextPos] == ")")
                nextPos++;
            return value;
        }

        if (decimal.TryParse(tokens[pos], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            nextPos = pos + 1;
            return number;
        }

        nextPos = pos + 1;
        return 0;
    }

    [GeneratedRegex(@"(\d+\.?\d*|\.\d+|[+\-*/()])", RegexOptions.Compiled)]
    private static partial Regex BuildTokenPattern();
}
