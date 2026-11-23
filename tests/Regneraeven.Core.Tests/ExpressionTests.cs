namespace Regneraeven.Core.Tests;

public class ExpressionTests
{
    [Theory]
    [InlineData("4 + 3 · 5", "19")]
    [InlineData("(4 + 3) · 5", "35")]
    [InlineData("4+3*5", "19")]
    [InlineData("20 - 2 x 3", "14")]
    [InlineData("3/4 + 1/2", "1 1/4")]
    [InlineData("2/3 · 3/4", "1/2")]
    [InlineData("2/3 : 4/5", "5/6")]
    [InlineData("3 · 2/5", "1 1/5")]
    [InlineData("12/4 + 1", "4")]
    [InlineData("2,5 · 4", "10")]
    [InlineData("1,5 · 2,4", "3,6")]
    [InlineData("0,1 + 0,2", "0,3")]
    [InlineData("12.50 + 3.75", "16,25")]
    [InlineData("17 : 5", "3 2/5")]
    [InlineData("5 − (−3)", "8")]
    [InlineData("-4 · (-3)", "12")]
    [InlineData("3 - 7", "-4")]
    [InlineData("1.500 + 1", "1501")]
    [InlineData("10 : 4", "2 1/2")]
    [InlineData("7,5 : 3", "2,5")]
    [InlineData("48 : (2 + 4) · 2", "16")]
    [InlineData("6/8", "3/4")]
    public void Solves(string input, string expected)
    {
        var result = Expression.Solve(input);
        Assert.Equal(expected, result.ResultText);
        Assert.NotEmpty(result.Steps);
    }

    [Fact]
    public void Steps_follow_the_order_of_operations()
    {
        var r = Expression.Solve("2 + (3 + 1) · 5");
        Assert.Equal(["2 + 4 · 5", "2 + 20", "22"], r.Steps.Select(s => s.Expression));
        Assert.StartsWith("Først det inde i parentesen", r.Steps[0].Explanation);
        Assert.StartsWith("Gange og division før plus og minus", r.Steps[1].Explanation);
    }

    [Fact]
    public void Fraction_addition_explains_common_denominator()
    {
        var r = Expression.Solve("1/2 + 1/3");
        Assert.Contains("fællesnævneren 6", r.Steps[0].Explanation);
        Assert.Contains("3/6 + 2/6 = 5/6", r.Steps[0].Explanation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("3 +")]
    [InlineData("(3 + 4")]
    [InlineData("hej")]
    [InlineData("3 / 0")]
    public void Rejects_bad_input(string input)
    {
        Assert.ThrowsAny<Exception>(() => Expression.Solve(input));
    }

    [Fact]
    public void Division_by_zero_inside_is_friendly()
    {
        var ex = Assert.Throws<FormatException>(() => Expression.Solve("5 : (2 - 2)"));
        Assert.Contains("0", ex.Message);
    }

    [Theory]
    [InlineData("3/4 + 1/6")]
    [InlineData("4 + 3 · 5")]
    [InlineData("432 : 6")]
    [InlineData("2,5 · 3,4")]
    [InlineData("(12 - 4) · 3 + 7")]
    public void Similar_example_has_same_shape_but_other_numbers(string input)
    {
        Assert.True(Expression.TryParse(input, out var node, out _));
        var rng = new Random(42);
        for (var i = 0; i < 50; i++)
        {
            var similar = Expression.MakeSimilar(node, rng);
            Assert.NotEqual(Expression.Render(node), Expression.Render(similar));
            Assert.Equal(node.GetType(), similar.GetType());
            _ = Expression.Solve(similar, Expression.Render(similar));
        }
    }

    [Fact]
    public void Similar_division_stays_whole_when_original_was()
    {
        Assert.True(Expression.TryParse("432 : 6", out var node, out _));
        var rng = new Random(1);
        for (var i = 0; i < 50; i++)
            Assert.True(Expression.Evaluate(Expression.MakeSimilar(node, rng)).IsInteger);
    }
}
