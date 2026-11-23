using System.Text.RegularExpressions;

namespace Regneraeven.Core.Help;

/// <summary>One piece to read aloud. <see cref="Section"/> is -1 for the intro and "Husk" line.</summary>
public sealed record SpeechChunk(int Section, string Text);

/// <summary>
/// Turns written maths into words a Danish voice reads well:
/// "3/4 + 1/6" → "tre fjerdedele plus en sjettedel", "4 · 5" → "4 gange 5", "36 cm²" → "36 kvadratcentimeter".
/// Column layouts (opstillinger) are not read aloud – the child is told to look at the screen.
/// </summary>
public static partial class SpeechText
{
    private static readonly Dictionary<int, (string One, string Many)> Parts = new()
    {
        [2] = ("halv", "halve"), [3] = ("tredjedel", "tredjedele"), [4] = ("fjerdedel", "fjerdedele"),
        [5] = ("femtedel", "femtedele"), [6] = ("sjettedel", "sjettedele"), [7] = ("syvendedel", "syvendedele"),
        [8] = ("ottendedel", "ottendedele"), [9] = ("niendedel", "niendedele"), [10] = ("tiendedel", "tiendedele"),
        [12] = ("tolvtedel", "tolvtedele"), [100] = ("hundrededel", "hundrededele"),
    };

    private static readonly Dictionary<int, string> Small = new()
    {
        [1] = "en", [2] = "to", [3] = "tre", [4] = "fire", [5] = "fem", [6] = "seks", [7] = "syv", [8] = "otte", [9] = "ni", [10] = "ti",
    };

    [GeneratedRegex(@"(?<![\d,.])(\d+) (\d+)/(\d+)(?![\d,])")] private static partial Regex MixedRx();
    [GeneratedRegex(@"(?<![\d,.])(\d+)/(\d+)(?![\d,])")] private static partial Regex FractionRx();
    [GeneratedRegex(@"\(([^()]*[a-zæøåA-ZÆØÅ]{3,}[^()]*)\)")] private static partial Regex CommentRx();
    [GeneratedRegex(@"\d \d")] private static partial Regex ColumnRx();
    [GeneratedRegex(@"\s{2,}")] private static partial Regex SpacesRx();

    /// <summary>"3/4" → "tre fjerdedele", "1/2" → "en halv", "7/13" → "7 over 13".</summary>
    public static string Fraction(int n, int d)
    {
        var count = Small.TryGetValue(n, out var w) ? w : n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (Parts.TryGetValue(d, out var part)) return $"{count} {(n == 1 ? part.One : part.Many)}";
        return $"{count} over {d}";
    }

    /// <summary>Normal text with a bit of maths in it.</summary>
    public static string Prose(string text)
    {
        var s = CommonMaths(text);
        // Parentheses in sentences are read as a pause, not as the word "parentes".
        s = s.Replace("(", ", ").Replace(")", ", ");
        return Tidy(s);
    }

    /// <summary>A calculation line like "(4 + 3) · 5" or "2 · x = 16     (træk 5 fra på begge sider)".</summary>
    public static string Formula(string line)
    {
        // Comments in brackets ("(træk 5 fra …)") are read as words.
        var s = CommentRx().Replace(line, m => ", " + m.Groups[1].Value + ", ");
        s = CommonMaths(s);
        s = s.Replace("(", " parentes ").Replace(")", " slut parentes ");
        if (s.TrimStart().StartsWith('=')) s = "er lig med " + s.TrimStart()[1..];
        s = s.Replace("=", " er lig med ");
        return Tidy(s);
    }

    /// <summary>True for column layouts (opstillinger) that don't make sense read aloud.</summary>
    public static bool IsLayout(IReadOnlyList<string> lines) =>
        lines.Any(l => l.Contains("---") || l.Contains('←') || l.Contains('¹') || ColumnRx().IsMatch(l.Trim()));

    private static string CommonMaths(string text)
    {
        var s = text.Replace('−', '-').Replace('–', '-');
        s = MixedRx().Replace(s, m => $"{m.Groups[1].Value} og {Fraction(int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value))}");
        s = FractionRx().Replace(s, m => Fraction(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));
        s = s.Replace("½", " en halv ");
        s = s.Replace("cm²", " kvadratcentimeter").Replace("m²", " kvadratmeter").Replace("°", " grader");
        s = s.Replace("·", " gange ").Replace("×", " gange ");
        s = Regex.Replace(s, @"(?<=[\d)x])\s*:\s*(?=[\d(])", " divideret med ");
        s = Regex.Replace(s, @"(?<=[\d)x])\s+-\s+(?=[\d(x])", " minus ");
        s = Regex.Replace(s, @"(?<=^|[\s(=,])-(?=\d)", "minus ");
        s = Regex.Replace(s, @"\s=\s", " er lig med ");
        s = s.Replace("+", " plus ").Replace("%", " procent").Replace("→", ", ").Replace("⇒", ", ").Replace("…", "");
        return s;
    }

    private static string Tidy(string s)
    {
        s = SpacesRx().Replace(s, " ").Trim();
        s = Regex.Replace(s, @"\s+([,.!?])", "$1");
        s = Regex.Replace(s, @",(\s*,)+", ",");
        s = Regex.Replace(s, @",\s*\.", ".");
        return s.Trim(' ', ',');
    }

    /// <summary>Everything to read for a rule page, section by section.</summary>
    public static IReadOnlyList<SpeechChunk> ForRule(RuleTopic rule)
    {
        var chunks = new List<SpeechChunk> { new(-1, $"{rule.Info.Name}. {Prose(rule.Intro)}") };
        for (var i = 0; i < rule.Sections.Count; i++) chunks.Add(new SpeechChunk(i, ForSection(rule.Sections[i])));
        chunks.Add(new SpeechChunk(-1, "Husk: " + Prose(rule.Remember)));
        return chunks;
    }

    public static string ForSection(RuleSection section)
    {
        var parts = new List<string> { section.Heading + ".", Prose(section.Text) };
        if (section.Example is { Count: > 0 } ex)
        {
            if (IsLayout(ex)) parts.Add("Se opstillingen på skærmen.");
            else parts.Add("For eksempel: " + string.Join(". ", ex.Select(Formula)) + ".");
        }
        if (section.Tip is not null) parts.Add("Tip: " + Prose(section.Tip));
        var text = string.Join(" ", parts.Select(p => p.Trim()).Where(p => p.Length > 0));
        return Tidy(Regex.Replace(text, @"\.\s*\.", "."));
    }
}
