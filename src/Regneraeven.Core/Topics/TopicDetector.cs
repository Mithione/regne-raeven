using System.Text.RegularExpressions;
using Regneraeven.Core.Maths;

namespace Regneraeven.Core.Topics;

/// <summary>Suggests tags for a problem the child saves, so it is easy to find again.</summary>
public static partial class TopicDetector
{
    [GeneratedRegex(@"\d\s*/\s*\d")] private static partial Regex FractionRx();
    [GeneratedRegex(@"\d,\d")] private static partial Regex DecimalRx();
    // A minus is a negative sign only at the start or right after "(", "=" or another operator – "12 - 4" is plain minus.
    [GeneratedRegex(@"(^|[(=+\-−·*×:/÷])\s*[-−]\s*\d|-\s*\d+\s*grader|minus\s*\d+\s*grader")] private static partial Regex NegativeRx();
    [GeneratedRegex(@"\b(km|m|dm|cm|mm|kg|g|l|dl|cl|ml)\b", RegexOptions.IgnoreCase)] private static partial Regex UnitRx();

    private static readonly (string Word, Topic Topic)[] Words =
    [
        ("areal", Topic.Geometri), ("omkreds", Topic.Geometri), ("vinkel", Topic.Geometri), ("trekant", Topic.Geometri),
        ("rektangel", Topic.Geometri), ("kvadrat", Topic.Geometri), ("cirkel", Topic.Geometri), ("grader", Topic.Geometri),
        ("koordinat", Topic.Koordinatsystem), ("punkt", Topic.Koordinatsystem), ("akse", Topic.Koordinatsystem),
        ("procent", Topic.Procent), ("rabat", Topic.Procent), ("%", Topic.Procent),
        ("brøk", Topic.Broeker), ("tæller", Topic.Broeker), ("nævner", Topic.Broeker), ("halvdel", Topic.Broeker),
        ("decimal", Topic.Decimaltal), ("ligning", Topic.Ligninger),
        ("minusgrader", Topic.NegativeTal), ("negativ", Topic.NegativeTal), ("frost", Topic.NegativeTal),
        ("omregn", Topic.Enheder),
        ("lægge sammen", Topic.Plus), ("i alt", Topic.Plus), ("tilsammen", Topic.Plus),
        ("forskel", Topic.Minus), ("tilbage", Topic.Minus),
        ("gange", Topic.Gange), ("hver har", Topic.Gange), ("ad gangen", Topic.Gange),
        ("dele", Topic.Division), ("fordel", Topic.Division), ("hver får", Topic.Division),
    ];

    public static IReadOnlyList<Topic> Suggest(string? text)
    {
        var found = new List<Topic>();
        if (string.IsNullOrWhiteSpace(text)) return found;
        void Add(Topic t) { if (!found.Contains(t)) found.Add(t); }

        var lower = text.ToLowerInvariant();
        var isExpression = Expression.TryParse(text, out var node, out _);

        if (isExpression)
        {
            var ops = new HashSet<char>();
            var hasGroup = false;
            Walk(node, ops, ref hasGroup);
            if (ops.Contains('+')) Add(Topic.Plus);
            if (ops.Contains('-')) Add(Topic.Minus);
            if (ops.Contains('*')) Add(Topic.Gange);
            if (ops.Contains('/')) Add(Topic.Division);
            if (hasGroup || ops.Count(o => o is '+' or '-') > 0 && ops.Count(o => o is '*' or '/') > 0) Add(Topic.Regnehierarki);
        }
        else
        {
            Add(Topic.Tekststykke);
            foreach (var (word, topic) in Words)
                if (lower.Contains(word)) Add(topic);
            if (UnitRx().IsMatch(lower) && !found.Contains(Topic.Geometri)) Add(Topic.Enheder);
        }

        if (FractionRx().IsMatch(text)) Add(Topic.Broeker);
        if (DecimalRx().IsMatch(text)) Add(Topic.Decimaltal);
        if (text.Contains('%')) Add(Topic.Procent);
        if (NegativeRx().IsMatch(text)) Add(Topic.NegativeTal);
        if (text.Contains('=') && lower.Contains('x')) { found.Remove(Topic.Tekststykke); Add(Topic.Ligninger); }

        // Fractions written with "/" are not division.
        if (found.Contains(Topic.Broeker) && isExpression && !text.Contains(':') && !text.Contains('÷')) found.Remove(Topic.Division);
        return found;
    }

    private static void Walk(Node node, HashSet<char> ops, ref bool hasGroup)
    {
        switch (node)
        {
            case Bin b: ops.Add(b.Op); Walk(b.Left, ops, ref hasGroup); Walk(b.Right, ops, ref hasGroup); break;
            case Maths.Group g: hasGroup = true; Walk(g.Inner, ops, ref hasGroup); break;
            case Neg n: Walk(n.Inner, ops, ref hasGroup); break;
        }
    }
}
