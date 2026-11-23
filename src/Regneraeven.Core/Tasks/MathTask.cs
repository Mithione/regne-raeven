using System.Globalization;
using System.Numerics;
using Regneraeven.Core.Maths;
using Regneraeven.Core.Topics;

namespace Regneraeven.Core.Tasks;

public enum AnswerKind
{
    /// <summary>A number; decimals and fractions of the same value are accepted.</summary>
    Number = 0,
    /// <summary>A fraction; equivalent fractions count as right, with a reminder to shorten.</summary>
    Fraction = 1,
    /// <summary>Must be shortened as much as possible (the task is "forkort brøken").</summary>
    ReducedFraction = 2,
    /// <summary>A point (x, y), typed or clicked in the coordinate system.</summary>
    Point = 3,
}

public sealed record GridPoint(string Name, int X, int Y);

/// <summary>Drawing that goes with a task: a shape with side labels, or a coordinate system.</summary>
public sealed record Visual
{
    /// <summary>"rect", "righttriangle", "triangle", "lshape" or "grid".</summary>
    public required string Kind { get; init; }
    public double[] Values { get; init; } = [];
    public string[] Labels { get; init; } = [];
    public int Min { get; init; }
    public int Max { get; init; }
    public List<GridPoint> Points { get; init; } = [];
    /// <summary>Grid only: the child answers by clicking a point.</summary>
    public bool Clickable { get; init; }
}

/// <summary>A generated task. Serialised to the database, so keep property names stable.</summary>
public sealed record MathTask
{
    public required Topic Topic { get; init; }
    public required int Difficulty { get; init; }
    public required string Prompt { get; init; }
    /// <summary>Big centred line, e.g. "3/4 + 1/6 = ?". Optional.</summary>
    public string? Display { get; init; }
    public required AnswerKind Kind { get; init; }
    /// <summary>Canonical answer: "n/d" (invariant) for numbers, "x;y" for points.</summary>
    public required string Answer { get; init; }
    /// <summary>The answer as shown to the child: "1 1/4", "2,5", "(3, -2)".</summary>
    public required string AnswerText { get; init; }
    public string? Unit { get; init; }
    public string? Hint { get; init; }
    public Visual? Visual { get; init; }
    public IReadOnlyList<string> Solution { get; init; } = [];

    public static string Canonical(Rational r) => r.Numerator.ToString(CultureInfo.InvariantCulture) + "/" + r.Denominator.ToString(CultureInfo.InvariantCulture);

    public static Rational FromCanonical(string s)
    {
        var parts = s.Split('/');
        return new Rational(BigInteger.Parse(parts[0], CultureInfo.InvariantCulture), BigInteger.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}

public sealed record CheckResult(bool Correct, string Message);

public static class AnswerChecker
{
    public static CheckResult Check(MathTask task, string? answer, string? x = null, string? y = null)
    {
        if (task.Kind == AnswerKind.Point)
        {
            if (!AnswerParser.TryParsePoint(x, y, out var p))
                return new CheckResult(false, "Skriv både x og y som hele tal – eller klik i koordinatsystemet.");
            var expected = task.Answer.Split(';').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();
            if (p.X == expected[0] && p.Y == expected[1]) return new CheckResult(true, "Helt rigtigt!");
            if (p.X == expected[1] && p.Y == expected[0]) return new CheckResult(false, "Tæt på! Husk: x (hen ad) kommer først, så y (op/ned).");
            return new CheckResult(false, "Ikke helt. Prøv igen.");
        }

        if (!AnswerParser.TryParse(answer, out var parsed))
            return new CheckResult(false, "Jeg kan ikke læse dit svar. Skriv et tal som 12, 2,5 eller 3/4.");

        var right = MathTask.FromCanonical(task.Answer);
        if (parsed.Value != right) return new CheckResult(false, "Ikke helt. Prøv igen.");

        if (task.Kind == AnswerKind.ReducedFraction && !right.IsInteger && !(parsed.WrittenAsFraction && parsed.IsReducedFraction))
            return new CheckResult(false, "Værdien er rigtig, men brøken skal forkortes helt.");

        if (task.Kind == AnswerKind.Fraction && parsed.WrittenAsFraction && !parsed.IsReducedFraction && !right.IsInteger
            && answer!.Contains('/') && !answer.Trim().Contains(' '))
        {
            var p = Expression.TryParse(answer.Trim(), out var node, out _) && node is FracLit f ? f : null;
            if (p is not null && !p.IsReduced)
                return new CheckResult(true, $"Rigtigt! Du kan også forkorte det til {right.ToFractionString()}.");
        }
        return new CheckResult(true, "Helt rigtigt!");
    }
}
