using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Regneraeven.Core.Maths;

/// <summary>A number typed by a child: 12, 2,5, 2.5, 3/4, 1 3/4, -7, "36 cm²", "25 %".</summary>
public sealed record ParsedAnswer(Rational Value, bool WrittenAsFraction, bool IsReducedFraction);

public static partial class AnswerParser
{
    [GeneratedRegex(@"^(-?\d+)\s+(\d+)\s*/\s*(\d+)$")]
    private static partial Regex Mixed();

    [GeneratedRegex(@"^(-?\d+)\s*/\s*(\d+)$")]
    private static partial Regex Fraction();

    [GeneratedRegex(@"^-?(\d+([.,]\d+)*|[.,]\d+)$")]
    private static partial Regex Plain();

    public static bool TryParse(string? text, out ParsedAnswer answer)
    {
        answer = null!;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = Clean(text);
        if (s.Length == 0) return false;

        var m = Mixed().Match(s);
        if (m.Success)
        {
            var whole = BigInteger.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var n = BigInteger.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            var d = BigInteger.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            if (d.IsZero) return false;
            var frac = new Rational(n, d);
            var value = whole.Sign < 0 || m.Groups[1].Value.StartsWith('-') ? new Rational(whole) - frac : new Rational(whole) + frac;
            answer = new ParsedAnswer(value, true, BigInteger.GreatestCommonDivisor(n, d).IsOne && n < d);
            return true;
        }

        m = Fraction().Match(s);
        if (m.Success)
        {
            var n = BigInteger.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var d = BigInteger.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            if (d.IsZero) return false;
            answer = new ParsedAnswer(new Rational(n, d), true, BigInteger.GreatestCommonDivisor(n, d).IsOne && !d.IsOne);
            return true;
        }

        if (Plain().IsMatch(s))
        {
            if (!Expression.TryParse(s, out var node, out _)) return false;
            answer = new ParsedAnswer(Expression.Evaluate(node), false, false);
            return true;
        }
        return false;
    }

    /// <summary>Removes units, spaces and the like: "36 cm²" → "36", "−4" → "-4".</summary>
    private static string Clean(string text)
    {
        var s = text.Trim()
            .Replace('−', '-').Replace('–', '-')
            .Replace(" ", " ");
        // Strip trailing units / symbols (cm, m², kr., %, °, grader).
        s = Regex.Replace(s, @"[\s]*[A-Za-zÆØÅæøå²³°%\.]+\.?$", m => m.Value.Trim().All(c => c == '.') ? m.Value : "");
        s = s.Trim();
        if (s.StartsWith("- ")) s = "-" + s[2..].TrimStart();
        return s;
    }

    /// <summary>Two numbers for a point: x and y.</summary>
    public static bool TryParsePoint(string? x, string? y, out (int X, int Y) point)
    {
        point = default;
        if (!int.TryParse(Clean(x ?? ""), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var px)) return false;
        if (!int.TryParse(Clean(y ?? ""), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var py)) return false;
        point = (px, py);
        return true;
    }
}
