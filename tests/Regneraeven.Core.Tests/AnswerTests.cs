namespace Regneraeven.Core.Tests;

public class AnswerTests
{
    [Theory]
    [InlineData("12", 12, 1)]
    [InlineData("2,5", 5, 2)]
    [InlineData("2.5", 5, 2)]
    [InlineData("5/2", 5, 2)]
    [InlineData("2 1/2", 5, 2)]
    [InlineData("-1 1/2", -3, 2)]
    [InlineData("36 cm²", 36, 1)]
    [InlineData("25 %", 25, 1)]
    [InlineData("−4", -4, 1)]
    [InlineData("- 4", -4, 1)]
    [InlineData("85°", 85, 1)]
    [InlineData("320 kr.", 320, 1)]
    [InlineData("1.500", 1500, 1)]
    [InlineData("0,75 kg", 3, 4)]
    public void Parses_kid_answers(string text, int n, int d)
    {
        Assert.True(AnswerParser.TryParse(text, out var a));
        Assert.Equal(new Rational(n, d), a.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ved ikke")]
    [InlineData("3/0")]
    public void Rejects_non_numbers(string text) => Assert.False(AnswerParser.TryParse(text, out _));

    private static MathTask Task(AnswerKind kind, Rational answer) => new()
    {
        Topic = Topic.Broeker, Difficulty = 1, Prompt = "", Kind = kind,
        Answer = MathTask.Canonical(answer), AnswerText = answer.ToMixedString(),
    };

    [Fact]
    public void Reduced_fraction_task_needs_shortening()
    {
        var t = Task(AnswerKind.ReducedFraction, new Rational(3, 4));
        Assert.False(AnswerChecker.Check(t, "6/8").Correct);
        Assert.True(AnswerChecker.Check(t, "3/4").Correct);
        Assert.False(AnswerChecker.Check(t, "2/3").Correct);
    }

    [Fact]
    public void Fraction_task_accepts_equivalent_with_tip()
    {
        var t = Task(AnswerKind.Fraction, new Rational(5, 4));
        var r = AnswerChecker.Check(t, "10/8");
        Assert.True(r.Correct);
        Assert.Contains("forkorte", r.Message);
        Assert.True(AnswerChecker.Check(t, "1 1/4").Correct);
        Assert.True(AnswerChecker.Check(t, "1,25").Correct);
    }

    [Fact]
    public void Point_answer_warns_about_swapped_coordinates()
    {
        var t = new MathTask { Topic = Topic.Koordinatsystem, Difficulty = 1, Prompt = "", Kind = AnswerKind.Point, Answer = "3;-2", AnswerText = "(3, -2)" };
        Assert.True(AnswerChecker.Check(t, null, "3", "-2").Correct);
        var swapped = AnswerChecker.Check(t, null, "-2", "3");
        Assert.False(swapped.Correct);
        Assert.Contains("x", swapped.Message);
    }
}
