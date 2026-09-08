using CurrencyCalculator.Web.Core;

// 使用产品中的解析器，检查键盘可输入的完整算式与真实错误边界。
(string Input, decimal? Expected)[] cases =
[
    ("(12+5)*18", 306m),
    ("(12)", 12m),
    ("((12))", 12m),
    ("-12+5", -7m),
    ("1.5/2", .75m),
    ("0", 0m),
    ("12+", null),
    ("(12", null),
    ("1/0", null),
    ("2abc+3", null),
    ("79228162514264337593543950335+1", null)
];
foreach (var (input, expected) in cases)
{
    var valid = MathExpressionEvaluator.TryEvaluate(input, out var result);
    if (valid != expected.HasValue || (valid && result != expected))
        throw new InvalidOperationException($"算式验证失败：{input}");
}
Console.WriteLine($"{cases.Length} 项算式验证通过。");
