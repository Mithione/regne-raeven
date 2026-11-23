using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Regneraeven.Core.Maths;

/// <summary>
/// Turns dictated Danish into a written math problem:
/// "tre fjerdedele plus en sjettedel" → "3/4 + 1/6", "to komma fem gange fire" → "2,5 · 4",
/// "start parentes tolv minus fire slut parentes gange tre" → "(12 - 4) · 3".
/// Speech-to-text often writes numbers as digits already; both forms work.
/// Returns null when the words are not a plain calculation (then the dictated text is kept as a word problem).
/// </summary>
public static partial class SpokenMath
{
    private static readonly Dictionary<string, int> Units = new()
    {
        ["nul"] = 0, ["en"] = 1, ["et"] = 1, ["én"] = 1, ["ét"] = 1, ["to"] = 2, ["tre"] = 3, ["fire"] = 4, ["fem"] = 5,
        ["seks"] = 6, ["syv"] = 7, ["otte"] = 8, ["ni"] = 9, ["ti"] = 10, ["elleve"] = 11, ["tolv"] = 12,
        ["tretten"] = 13, ["fjorten"] = 14, ["femten"] = 15, ["seksten"] = 16, ["sytten"] = 17, ["atten"] = 18, ["nitten"] = 19,
    };

    private static readonly Dictionary<string, int> Tens = new()
    {
        ["tyve"] = 20, ["tredive"] = 30, ["fyrre"] = 40, ["fyrretyve"] = 40, ["halvtreds"] = 50, ["halvtredsindstyve"] = 50,
        ["tres"] = 60, ["tresindstyve"] = 60, ["halvfjerds"] = 70, ["halvfjerdsindstyve"] = 70, ["firs"] = 80, ["firsindstyve"] = 80,
        ["halvfems"] = 90, ["halvfemsindstyve"] = 90,
    };

    /// <summary>Denominator words: "fjerdedel(e)" → 4. "halv"/"halve" and "kvart"/"kvarte" too.</summary>
    private static readonly (string Stem, int Den)[] Denominators =
    [
        ("halvdel", 2), ("halve", 2), ("halv", 2), ("tredjedel", 3), ("fjerdedel", 4), ("kvarte", 4), ("kvart", 4),
        ("femtedel", 5), ("sjettedel", 6), ("syvendedel", 7), ("ottendedel", 8), ("niendedel", 9), ("tiendedel", 10),
        ("ellevtedel", 11), ("tolvtedel", 12), ("tyvendedel", 20), ("hundrededel", 100), ("tusindedel", 1000),
    ];

    /// <summary>Spoken operators, longest first. Values are written symbols.</summary>
    private static readonly (string[] Words, string Symbol)[] Operators =
    [
        (["er", "lig", "med"], "="), (["lig", "med"], "="), (["ganget", "med"], "·"), (["gange", "med"], "·"), (["multipliceret", "med"], "·"),
        (["divideret", "med"], ":"), (["delt", "med"], ":"), (["delt", "i"], ":"), (["divideret"], ":"), (["delt"], ":"),
        (["start", "parentes"], "("), (["begynd", "parentes"], "("), (["parentes", "start"], "("), (["åben", "parentes"], "("), (["parentes", "begynd"], "("),
        (["slut", "parentes"], ")"), (["luk", "parentes"], ")"), (["parentes", "slut"], ")"),
        (["plus"], "+"), (["minus"], "-"), (["gange"], "·"), (["giver"], "="), (["procent"], "%"),
    ];

    /// <summary>Words that carry no math ("hvad er … ?", "regn ud …").</summary>
    private static readonly HashSet<string> Filler = ["hvad", "regn", "ud", "udregn", "beregn", "spørgsmålstegn", "resultatet", "af", "så", "lige", "øh", "hmm"];

    [GeneratedRegex(@"\d+(?:[.,]\d+)?(?:/\d+)?|[a-zæøå]+|[+\-−–*·×x:/÷()=%?]", RegexOptions.IgnoreCase)]
    private static partial Regex TokenRx();

    public static string? ToExpression(string? spoken)
    {
        if (string.IsNullOrWhiteSpace(spoken)) return null;
        var tokens = TokenRx().Matches(spoken.ToLowerInvariant()).Select(m => m.Value).ToList();
        if (tokens.Count == 0) return null;

        var output = new List<string>();
        var openParens = 0;
        var i = 0;
        var sawOperator = false;

        bool LastIsValue() => output.Count > 0 && (IsNumber(output[^1]) || output[^1] is ")" or "x" or "%");

        while (i < tokens.Count)
        {
            var t = tokens[i];

            // Operators spoken as words.
            var op = MatchOperator(tokens, i);
            if (op is not null)
            {
                var (symbol, length) = op.Value;
                i += length;
                if (symbol == "=" && output.Count == 0) continue; // "er ..." at the start
                if (symbol == "(") openParens++;
                if (symbol == ")") { if (openParens == 0) continue; openParens--; }
                if (symbol is "(" && LastIsValue()) output.Add("·");
                output.Add(symbol);
                if (symbol is not "(" and not ")" and not "%") sawOperator = true;
                continue;
            }

            // "hvad er 7 gange 8" (skip) / "to plus to er fire" (=)
            if (t == "er")
            {
                i++;
                if (output.Count > 0 && LastIsValue()) { output.Add("="); sawOperator = true; }
                continue;
            }

            // Lone "parentes": open, or close if one is open.
            if (t == "parentes")
            {
                i++;
                if (openParens > 0) { output.Add(")"); openParens--; }
                else { if (LastIsValue()) output.Add("·"); output.Add("("); openParens++; }
                continue;
            }

            // Written symbols from speech-to-text ("3 + 4", "3 x 4").
            if (t.Length == 1 && "+-−–*·×:/÷()=%".Contains(t[0]))
            {
                var symbol = t switch { "−" or "–" => "-", "*" or "×" => "·", "/" or "÷" => ":", _ => t };
                if (symbol == "(") { if (LastIsValue()) output.Add("·"); openParens++; }
                if (symbol == ")") { if (openParens == 0) { i++; continue; } openParens--; }
                output.Add(symbol);
                if (symbol is not "(" and not ")" and not "%") sawOperator = true;
                i++;
                continue;
            }
            if (t == "?") { i++; continue; }

            // "x": times between two numbers, otherwise the unknown in an equation.
            if (t == "x")
            {
                var nextIsNumber = i + 1 < tokens.Count && (IsNumber(tokens[i + 1]) || TryReadNumber(tokens, i + 1, out _, out _));
                if (LastIsValue() && nextIsNumber && output[^1] != "x") { output.Add("·"); sawOperator = true; }
                else { if (LastIsValue()) output.Add("·"); output.Add("x"); }
                i++;
                continue;
            }

            // Numbers (digits or words), maybe followed by "komma …", a fraction word or "og en halv".
            if (TryReadValue(tokens, i, out var value, out var used))
            {
                if (LastIsValue()) return null; // two numbers in a row – not a calculation we understand
                output.Add(value);
                i += used;
                continue;
            }

            if (Filler.Contains(t)) { i++; continue; }

            return null; // a real word – this is a text problem, not a calculation
        }

        while (openParens-- > 0) output.Add(")");
        while (output.Count > 0 && output[^1] is "=" or "+" or "-" or "·" or ":") output.RemoveAt(output.Count - 1);
        if (!sawOperator || output.Count < 3) return null;

        var text = Join(output);
        // Equations (x) and percent can't be checked by the expression parser – hand them back as written.
        if (text.Contains('x') || text.Contains('%')) return text;
        // Otherwise only hand back something the rest of the program can calculate.
        return Expression.TryParse(text.Split('=')[0].Trim(), out _, out _) ? text : null;
    }

    private static string Join(List<string> parts)
    {
        var sb = new StringBuilder();
        for (var k = 0; k < parts.Count; k++)
        {
            var p = parts[k];
            if (k > 0 && p is not ")" and not "%" && parts[k - 1] != "(" && !IsUnaryAt(parts, k - 1)) sb.Append(' ');
            sb.Append(p);
        }
        return sb.ToString();
    }

    private static bool IsUnaryAt(List<string> parts, int k) =>
        parts[k] == "-" && (k == 0 || parts[k - 1] is "(" or "+" or "-" or "·" or ":" or "=");

    private static (string Symbol, int Length)? MatchOperator(List<string> tokens, int i)
    {
        foreach (var (words, symbol) in Operators)
        {
            if (i + words.Length > tokens.Count) continue;
            var ok = true;
            for (var k = 0; k < words.Length && ok; k++) ok = tokens[i + k] == words[k];
            if (ok) return (symbol, words.Length);
        }
        return null;
    }

    private static bool IsNumber(string s) => s.Length > 0 && char.IsDigit(s[0]);

    /// <summary>A number with optional decimals, fraction or "og en halv": "to komma fem", "tre fjerdedele", "2 og en halv".</summary>
    private static bool TryReadValue(List<string> tokens, int i, out string value, out int used)
    {
        value = "";
        used = 0;

        // "halvanden" = 1 1/2
        if (tokens[i] == "halvanden") { value = "(1 + 1/2)"; used = 1; return true; }

        if (!TryReadNumber(tokens, i, out var number, out used)) return false;
        var text = number;

        // Decimal: "to komma fem" / "2 komma 25"
        if (i + used < tokens.Count && tokens[i + used] == "komma" && !number.Contains(',') && !number.Contains('/'))
        {
            var j = i + used + 1;
            var digits = new StringBuilder();
            // Decimals are said digit by digit ("komma nul fem") or as one number ("komma femogtyve").
            while (j < tokens.Count && TryReadNumber(tokens, j, out var d, out var u) && !d.Contains('/') && !d.Contains(','))
            {
                digits.Append(d);
                j += u;
                if (d.Length > 1) break;
            }
            if (digits.Length > 0) { text = number + "," + digits; used = j - i; }
        }

        // Fraction word: "tre fjerdedele", "en halv", "3 femtedele"
        if (i + used < tokens.Count && !text.Contains(',') && !text.Contains('/') && Denominator(tokens[i + used]) is { } den)
        {
            text = $"{number}/{den}";
            used++;
        }
        // "over": "tre over fire"
        else if (i + used + 1 < tokens.Count && tokens[i + used] == "over" && !text.Contains(',') && TryReadNumber(tokens, i + used + 1, out var under, out var u2) && !under.Contains(','))
        {
            text = $"{number}/{under}";
            used += 1 + u2;
        }

        // Mixed number: "to og en halv" → (2 + 1/2)
        if (!text.Contains('/') && i + used + 2 < tokens.Count && tokens[i + used] == "og"
            && TryReadNumber(tokens, i + used + 1, out var n2, out var u3) && i + used + 1 + u3 < tokens.Count
            && Denominator(tokens[i + used + 1 + u3]) is { } den2)
        {
            text = $"({text} + {n2}/{den2})";
            used += 2 + u3;
        }

        value = text;
        return true;
    }

    private static int? Denominator(string word)
    {
        foreach (var (stem, den) in Denominators)
            if (word == stem || word == stem + "e" || word == stem + "en" || word == stem + "ene") return den;
        return null;
    }

    /// <summary>Reads a whole number in digits or Danish words: "125", "hundrede og femogtyve", "to tusind tre hundrede".</summary>
    private static bool TryReadNumber(List<string> tokens, int i, out string number, out int used)
    {
        number = "";
        used = 0;
        if (i >= tokens.Count) return false;

        var t = tokens[i];
        if (IsNumber(t))
        {
            number = t.Contains('.') && !t.Contains(',') && Regex.IsMatch(t, @"^\d+\.\d+$") && t.Split('.')[1].Length != 3 ? t.Replace('.', ',') : t;
            if (Regex.IsMatch(number, @"^\d{1,3}(\.\d{3})+$")) number = number.Replace(".", "");
            used = 1;
            return true;
        }

        long total = 0, current = 0;
        var j = i;
        var any = false;
        var lastWasSmall = false; // last word was 0–19 or e.g. "femogtyve" – another one starts a new number
        while (j < tokens.Count)
        {
            var w = tokens[j];
            if (w == "og" && any && j + 1 < tokens.Count && WordValue(tokens[j + 1]) is not null && Denominator(tokens.ElementAtOrDefault(j + 2) ?? "") is null)
            {
                j++;
                continue;
            }
            if (w is "hundrede" or "hundred")
            {
                current = (current == 0 ? 1 : current) * 100;
                any = true; lastWasSmall = false; j++;
                continue;
            }
            if (w is "tusind" or "tusinde")
            {
                total += (current == 0 ? 1 : current) * 1000;
                current = 0;
                any = true; lastWasSmall = false; j++;
                continue;
            }
            if (WordValue(w) is { } v)
            {
                // "to tre" or "nul fem" is two numbers, not one – but "fem og tyve" is 25.
                var tensAfterOg = j > i && tokens[j - 1] == "og" && Tens.ContainsKey(w) && current % 100 < 10;
                if (lastWasSmall && !tensAfterOg) break;
                current += v;
                any = true; lastWasSmall = true; j++;
                continue;
            }
            break;
        }
        if (!any) return false;
        // "en" alone before a fraction word is fine; "en" alone otherwise is still 1.
        number = (total + current).ToString(CultureInfo.InvariantCulture);
        used = j - i;
        return true;
    }

    /// <summary>"femogtyve" → 25, "tyve" → 20, "syv" → 7.</summary>
    private static int? WordValue(string w)
    {
        if (Units.TryGetValue(w, out var u)) return u;
        if (Tens.TryGetValue(w, out var t)) return t;
        var og = w.IndexOf("og", StringComparison.Ordinal);
        if (og > 0)
        {
            var a = w[..og];
            var b = w[(og + 2)..];
            if (Units.TryGetValue(a, out var ua) && ua is > 0 and < 10 && Tens.TryGetValue(b, out var tb)) return ua + tb;
        }
        return null;
    }
}
