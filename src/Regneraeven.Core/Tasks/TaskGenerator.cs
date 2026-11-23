using System.Globalization;
using Regneraeven.Core.Maths;
using Regneraeven.Core.Topics;

namespace Regneraeven.Core.Tasks;

/// <summary>
/// Makes tasks at 5.–6. klasse level. Difficulty 1 = let, 2 = middel, 3 = svær.
/// Every generator computes its answer with <see cref="Rational"/>, so answers are exact.
/// </summary>
public sealed class TaskGenerator(Random rng)
{
    public static readonly IReadOnlyDictionary<int, string> DifficultyNames = new Dictionary<int, string>
    {
        [1] = "Let", [2] = "Middel", [3] = "Svær",
    };

    public MathTask Create(Topic topic, int difficulty)
    {
        difficulty = Math.Clamp(difficulty, 1, 3);
        return topic switch
        {
            Topic.Plus => Plus(difficulty),
            Topic.Minus => Minus(difficulty),
            Topic.Gange => Gange(difficulty),
            Topic.Division => Division(difficulty),
            Topic.Regnehierarki => Hierarki(difficulty),
            Topic.NegativeTal => Negative(difficulty),
            Topic.Decimaltal => Decimals(difficulty),
            Topic.Enheder => Units(difficulty),
            Topic.Broeker => Fractions(difficulty),
            Topic.Procent => Percent(difficulty),
            Topic.Ligninger => Equations(difficulty),
            Topic.Geometri => Geometry(difficulty),
            Topic.Koordinatsystem => Coordinates(difficulty),
            _ => throw new ArgumentException($"Der findes ingen opgaver af typen {topic}.", nameof(topic)),
        };
    }

    // ------------------------------------------------------------ helpers

    private int R(int min, int maxInclusive) => rng.Next(min, maxInclusive + 1);
    private T Pick<T>(params T[] items) => items[rng.Next(items.Length)];
    private static string F(Rational r) => Expression.FormatValue(r, r.IsInteger ? NumForm.Integer : r.HasFiniteDecimals ? NumForm.Decimal : NumForm.Fraction, mixed: true);
    private static string D(decimal d) => Rational.FromDecimal(d).ToDecimalString();
    private static string I(long n) => Rational.FormatInteger(n);

    /// <summary>A plain "calculate this" task solved by the step engine.</summary>
    private MathTask Expr(Topic topic, int difficulty, string expression, string? hint, AnswerKind kind = AnswerKind.Number, string? prompt = null)
    {
        var solved = Expression.Solve(expression);
        return new MathTask
        {
            Topic = topic,
            Difficulty = difficulty,
            Prompt = prompt ?? "Regn ud:",
            Display = Expression.Render(Parse(expression)) + " = ?",
            Kind = kind,
            Answer = MathTask.Canonical(solved.Value),
            AnswerText = solved.ResultText,
            Hint = hint,
            Solution = solved.Steps.Select(s => s.Explanation).ToList(),
        };
    }

    private static Node Parse(string expression) =>
        Expression.TryParse(expression, out var n, out var e) ? n : throw new InvalidOperationException(e);

    private static MathTask Simple(Topic topic, int difficulty, string prompt, Rational answer, string? unit, string? hint, IReadOnlyList<string> solution,
        string? display = null, Visual? visual = null, AnswerKind kind = AnswerKind.Number) => new()
    {
        Topic = topic,
        Difficulty = difficulty,
        Prompt = prompt,
        Display = display,
        Kind = kind,
        Answer = MathTask.Canonical(answer),
        AnswerText = (kind is AnswerKind.Fraction or AnswerKind.ReducedFraction ? answer.ToMixedString() : F(answer)) + (unit is null ? "" : " " + unit),
        Unit = unit,
        Hint = hint,
        Solution = solution,
        Visual = visual,
    };

    // ------------------------------------------------------------ plus / minus

    private MathTask Plus(int d)
    {
        var (a, b) = d switch
        {
            1 => (R(125, 689), R(118, 299)),
            2 => (R(1250, 6899), R(1180, 2999)),
            _ => (R(12_500, 68_999), R(2_500, 29_999)),
        };
        var c = d == 3 && rng.Next(2) == 0 ? R(150, 999) : 0;
        var sum = a + b + c;
        var expr = c > 0 ? $"{I(a)} + {I(b)} + {I(c)}" : $"{I(a)} + {I(b)}";
        return Simple(Topic.Plus, d, "Regn ud:", sum, null,
            "Stil tallene op under hinanden, så enerne står under enerne. Start fra højre, og husk mente.",
            [
                "Stil op: enere under enere, tiere under tiere osv.",
                "Læg sammen fra højre mod venstre. Bliver en kolonne 10 eller mere, skriver du enerne og giver 1 i mente til næste kolonne.",
                $"{expr} = {I(sum)}",
            ], display: expr + " = ?");
    }

    private MathTask Minus(int d)
    {
        int a, b;
        switch (d)
        {
            case 1: a = R(312, 899); b = R(128, a - 50); break;
            case 2: a = Pick(R(1000, 9000) / 1000 * 1000 + R(1, 9), R(2001, 9499)); b = R(1128, Math.Max(1200, a - 300)); break;
            default: a = R(20_000, 90_000); b = R(4_000, a - 2_000); break;
        }
        if (b >= a) (a, b) = (b + 100, a);
        var expr = $"{I(a)} − {I(b)}";
        return Simple(Topic.Minus, d, "Regn ud:", a - b, null,
            "Stil op med det største tal øverst. Er cifferet foroven for lille, så lån 1 fra kolonnen til venstre.",
            [
                "Stil op: det største tal øverst, enere under enere.",
                "Træk fra kolonne for kolonne fra højre. Mangler du, så lån 1 tier (= 10 enere) fra kolonnen til venstre.",
                $"Tjek: {I(a - b)} + {I(b)} = {I(a)}",
                $"{expr} = {I(a - b)}",
            ], display: expr + " = ?");
    }

    // ------------------------------------------------------------ gange / division

    private MathTask Gange(int d)
    {
        int a, b;
        switch (d)
        {
            case 1: a = R(3, 12); b = R(3, 10); break;
            case 2: a = R(13, 99); b = R(3, 9); break;
            default: (a, b) = Pick((R(12, 49), R(12, 39)), (R(102, 499), R(3, 9))); break;
        }
        var expr = $"{I(a)} · {I(b)}";
        var ones = a % 10;
        List<string> solution = d == 1
            ? [$"Brug {b}-tabellen: {expr} = {I(a * b)}"]
            : b < 10
                ? [$"Del {I(a)} op: {I(a)} = {I(a - ones)} + {ones}", $"{I(a - ones)} · {b} = {I((a - ones) * b)} og {ones} · {b} = {ones * b}", $"Læg sammen: {I((a - ones) * b)} + {ones * b} = {I(a * b)}"]
                : [$"Del {b} op: {b} = {b / 10 * 10} + {b % 10}", $"{I(a)} · {b / 10 * 10} = {I(a * (b / 10 * 10))} og {I(a)} · {b % 10} = {I(a * (b % 10))}", $"Læg sammen: {I(a * (b / 10 * 10))} + {I(a * (b % 10))} = {I(a * b)}"];
        return Simple(Topic.Gange, d, "Regn ud:", a * b, null,
            d == 1 ? "Kan du tabellen udenad? Ellers tæl i spring." : "Del det ene tal op i tiere og enere, og gang hver del for sig.",
            solution, display: expr + " = ?");
    }

    private MathTask Division(int d)
    {
        int divisor, quotient;
        switch (d)
        {
            case 1: divisor = R(3, 10); quotient = R(3, 12); break;
            case 2: divisor = R(3, 9); quotient = R(14, 99); break;
            default: (divisor, quotient) = Pick((R(11, 25), R(12, 40)), (R(4, 9), R(112, 999))); break;
        }
        var dividend = divisor * quotient;
        var expr = $"{I(dividend)} : {divisor}";
        return Simple(Topic.Division, d, "Regn ud:", quotient, null,
            $"Tænk baglæns: hvilket tal gange {divisor} giver {I(dividend)}?",
            [
                $"Spørg: hvor mange gange går {divisor} op i {I(dividend)}?",
                d == 1 ? $"{divisor} · {quotient} = {I(dividend)}" : "Del fra venstre: tag så mange cifre, at divisoren kan gå op, skriv resultatet og fortsæt med resten.",
                $"Tjek med gange: {I(quotient)} · {divisor} = {I(dividend)}",
                $"{expr} = {I(quotient)}",
            ], display: expr + " = ?");
    }

    // ------------------------------------------------------------ rækkefølge / negative tal

    private MathTask Hierarki(int d)
    {
        string expr;
        switch (d)
        {
            case 1:
                expr = Pick($"{R(2, 20)} + {R(2, 9)} · {R(2, 9)}", $"{R(30, 60)} − {R(2, 5)} · {R(2, 6)}", $"{R(2, 9)} · {R(2, 9)} + {R(2, 20)}");
                break;
            case 2:
                {
                    var c = R(2, 6);
                    expr = Pick($"({R(2, 12)} + {R(2, 12)}) · {c}", $"{R(40, 90)} − ({R(2, 9)} + {R(2, 9)}) · 2", $"{R(2, 6) * c} : {c} + {R(2, 9)} · {R(2, 5)}");
                    break;
                }
            default:
                {
                    var e = R(2, 5);
                    var b = R(8, 15);
                    expr = Pick($"{R(2, 6)} · ({b} − {R(2, b - 2)}) + {R(2, 9) * e} : {e}", $"({R(10, 30)} − {R(2, 9)}) · 2 + {R(2, 9)} · ({R(2, 5)} + {R(2, 5)})", $"{R(50, 99)} − {R(2, 6)} · {R(2, 6)} + ({R(2, 9)} + {R(2, 9)}) · 3");
                    break;
                }
        }
        var t = Expr(Topic.Regnehierarki, d, expr, "Parenteser først. Så gange og division. Til sidst plus og minus – fra venstre mod højre.");
        return Expression.Evaluate(Parse(expr)).Sign < 0 ? Hierarki(d) : t;
    }

    private MathTask Negative(int d)
    {
        if (d == 1 && rng.Next(2) == 0)
        {
            var start = R(-3, 6);
            var fall = R(Math.Max(3, start + 2), start + 12);
            var end = start - fall;
            return Simple(Topic.NegativeTal, d,
                $"Om morgenen er der {I(start)} grader. I løbet af natten falder temperaturen {fall} grader. Hvad er temperaturen så?",
                end, "grader", "Brug en tallinje (et termometer): gå nedad for at falde.",
                [$"Start ved {I(start)} og gå {fall} ned ad tallinjen.", $"{I(start)} − {fall} = {I(end)}", $"Temperaturen er {I(end)} grader."]);
        }
        var expr = d switch
        {
            1 => Pick($"{R(2, 9)} − {R(10, 19)}", $"-{R(2, 9)} + {R(2, 15)}", $"-{R(2, 9)} − {R(2, 9)}"),
            2 => Pick($"-{R(5, 20)} + {R(2, 9)} − {R(5, 15)}", $"{R(2, 9)} − ({R(10, 19)} − {R(2, 5)})", $"-{R(2, 9)} · {R(2, 6)}"),
            _ => Pick($"-{R(2, 9)} − (-{R(2, 9)}) + {R(2, 9)}", $"-{R(2, 6)} · (-{R(2, 6)}) − {R(10, 40)}", $"({R(2, 9)} − {R(10, 19)}) · {R(2, 5)}"),
        };
        return Expr(Topic.NegativeTal, d, expr, "Tænk på tallinjen: plus går til højre, minus går til venstre. Minus et negativt tal er det samme som plus.");
    }

    // ------------------------------------------------------------ decimaltal / enheder

    private decimal Dec(int min, int max, int decimals)
    {
        var scale = (int)Math.Pow(10, decimals);
        int n;
        do n = R(min * scale, max * scale); while (n % 10 == 0);
        return (decimal)n / scale;
    }

    private MathTask Decimals(int d)
    {
        string expr;
        string hint;
        switch (d)
        {
            case 1:
                expr = Pick($"{D(Dec(1, 20, 1))} + {D(Dec(1, 20, 2))}", $"{D(Dec(10, 30, 2))} − {D(Dec(1, 9, 1))}");
                hint = "Stil op, så kommaerne står lige under hinanden. Mangler der et ciffer, kan du skrive et 0.";
                break;
            case 2:
                expr = Pick($"{D(Dec(1, 9, 1))} · {R(3, 9)}", $"{D(Dec(1, 50, 2))} · {Pick(10, 100, 1000)}", $"{D(Dec(10, 900, 1))} : {Pick(10, 100)}");
                hint = "Gange med 10, 100, 1000: flyt kommaet 1, 2 eller 3 pladser til højre. Division: til venstre.";
                break;
            default:
                {
                    var q = Dec(1, 9, 1);
                    var div = R(2, 6);
                    expr = Pick($"{D(Dec(1, 9, 1))} · {D(Dec(1, 9, 1))}", $"{D(q * div)} : {div}", $"{D(Dec(1, 20, 2))} + {D(Dec(1, 9, 1))} · {R(2, 5)}");
                    hint = "Ved gange: regn uden komma, og tæl så hvor mange decimaler der var i alt.";
                    break;
                }
        }
        return Expr(Topic.Decimaltal, d, expr, hint);
    }

    private sealed record Conversion(string From, string To, int Factor);

    private static readonly Conversion[] EasyConversions =
    [
        new("m", "cm", 100), new("cm", "mm", 10), new("kg", "g", 1000), new("l", "dl", 10), new("km", "m", 1000), new("l", "ml", 1000),
    ];

    private MathTask Units(int d)
    {
        var c = Pick(EasyConversions);
        var down = rng.Next(2) == 0; // From big to small unit?
        Rational value;
        string prompt, unit;
        List<string> sol;
        switch (d)
        {
            case 1:
                {
                    var n = R(2, 30);
                    if (down) { value = n * c.Factor; prompt = $"Hvor mange {c.To} er {I(n)} {c.From}?"; unit = c.To; sol = [$"1 {c.From} = {I(c.Factor)} {c.To}", $"{I(n)} · {I(c.Factor)} = {I(n * c.Factor)}"]; }
                    else { value = n; prompt = $"Hvor mange {c.From} er {I(n * c.Factor)} {c.To}?"; unit = c.From; sol = [$"1 {c.From} = {I(c.Factor)} {c.To}", $"{I(n * c.Factor)} : {I(c.Factor)} = {I(n)}"]; }
                    break;
                }
            case 2:
                {
                    var n = Rational.FromDecimal(Dec(1, 9, 1));
                    if (down) { value = n * c.Factor; prompt = $"Hvor mange {c.To} er {F(n)} {c.From}?"; unit = c.To; sol = [$"1 {c.From} = {I(c.Factor)} {c.To}", $"{F(n)} · {I(c.Factor)} = {F(value)} (flyt kommaet)"]; }
                    else { var small = R(1, c.Factor - 1) + c.Factor * R(0, 3); value = new Rational(small, c.Factor); prompt = $"Hvor mange {c.From} er {I(small)} {c.To}? Svar med decimaltal."; unit = c.From; sol = [$"1 {c.From} = {I(c.Factor)} {c.To}", $"{I(small)} : {I(c.Factor)} = {F(value)}"]; }
                    break;
                }
            default:
                {
                    var big = Rational.FromDecimal(Dec(1, 4, 2));
                    var small = R(1, 9) * (c.Factor / 10 == 0 ? 1 : c.Factor / 10);
                    value = big * c.Factor + small;
                    prompt = $"Læg sammen og svar i {c.To}: {F(big)} {c.From} + {I(small)} {c.To}";
                    unit = c.To;
                    sol = [$"Lav først alt om til {c.To}: {F(big)} {c.From} = {F(big * c.Factor)} {c.To}", $"{F(big * c.Factor)} + {I(small)} = {F(value)} {c.To}"];
                    break;
                }
        }
        return Simple(Topic.Enheder, d, prompt, value, unit, $"Husk: 1 {c.From} = {I(c.Factor)} {c.To}. Fra stor til lille enhed ganger du; fra lille til stor dividerer du.", sol);
    }

    // ------------------------------------------------------------ brøker / procent

    private MathTask Fractions(int d)
    {
        switch (d)
        {
            case 1:
                switch (rng.Next(3))
                {
                    case 0:
                        {
                            var den = Pick(3, 4, 5, 6, 7);
                            var num = R(1, den - 1);
                            var k = R(2, 5);
                            var r = new Rational(num, den);
                            return Simple(Topic.Broeker, d, "Forkort brøken så meget som muligt:", r, null,
                                "Find et tal, som både tæller og nævner kan deles med.",
                                [$"Både {num * k} og {den * k} kan deles med {k}.", $"{num * k} : {k} = {num} og {den * k} : {k} = {den}", $"{num * k}/{den * k} = {r.ToFractionString()}"],
                                display: $"{num * k}/{den * k} = ?", kind: AnswerKind.ReducedFraction);
                        }
                    case 1:
                        {
                            var den = Pick(5, 7, 8, 9, 11);
                            var a = R(1, den - 2);
                            var b = R(1, den - a - 1);
                            return Expr(Topic.Broeker, d, $"{a}/{den} + {b}/{den}", "Samme nævner: læg tællerne sammen og behold nævneren.", AnswerKind.Fraction);
                        }
                    default:
                        {
                            var den = Pick(2, 3, 4, 5, 10);
                            var total = den * R(2, 12);
                            var part = total / den;
                            return Simple(Topic.Broeker, d, $"Hvad er 1/{den} af {total}?", part, null, $"1/{den} af et tal: del tallet med {den}.",
                                [$"1/{den} af {total} = {total} : {den} = {part}"]);
                        }
                }
            case 2:
                switch (rng.Next(3))
                {
                    case 0:
                        {
                            var (d1, d2) = Pick((2, 4), (3, 6), (2, 6), (4, 8), (5, 10), (3, 9), (2, 10));
                            var a = R(1, d1 - 1);
                            var b = R(1, d2 - 1);
                            var op = new Rational(a, d1) > new Rational(b, d2) && rng.Next(2) == 0 ? "-" : "+";
                            return Expr(Topic.Broeker, d, $"{a}/{d1} {op} {b}/{d2}", "Lav brøkerne om, så de har samme nævner (fællesnævner).", AnswerKind.Fraction);
                        }
                    case 1:
                        {
                            var den = Pick(3, 4, 5, 8, 10);
                            var num = R(2, den - 1);
                            var total = den * R(3, 15);
                            var res = total / den * num;
                            return Simple(Topic.Broeker, d, $"Hvad er {num}/{den} af {total}?", res, null,
                                $"Find først 1/{den} (del med {den}), og gang så med {num}.",
                                [$"1/{den} af {total} = {total} : {den} = {total / den}", $"{num}/{den} af {total} = {total / den} · {num} = {res}"]);
                        }
                    default:
                        {
                            var den = Pick(2, 3, 4, 5);
                            var num = R(1, den - 1);
                            var k = R(2, 6);
                            return Simple(Topic.Broeker, d, $"Forlæng brøken: {num}/{den} = ?/{den * k}. Hvad skal tælleren være?", num * k, null,
                                $"Hvad har nævneren {den} er ganget med for at blive {den * k}? Gang tælleren med det samme.",
                                [$"{den} · {k} = {den * k}", $"Tælleren ganges også med {k}: {num} · {k} = {num * k}", $"{num}/{den} = {num * k}/{den * k}"],
                                display: $"{num}/{den} = ?/{den * k}");
                        }
                }
            default:
                switch (rng.Next(3))
                {
                    case 0:
                        {
                            var (d1, d2) = Pick((2, 3), (3, 4), (2, 5), (3, 5), (4, 5), (4, 6), (6, 8));
                            var a = R(1, d1 - 1);
                            var b = R(1, d2 - 1);
                            var op = new Rational(a, d1) > new Rational(b, d2) ? Pick("+", "-") : "+";
                            return Expr(Topic.Broeker, d, $"{a}/{d1} {op} {b}/{d2}", "Find den mindste fællesnævner – et tal som begge nævnere går op i.", AnswerKind.Fraction);
                        }
                    case 1:
                        {
                            var d1 = Pick(2, 3, 4, 5);
                            var d2 = Pick(3, 4, 5, 6);
                            return Expr(Topic.Broeker, d, $"{R(1, d1 - 1)}/{d1} · {R(1, d2 - 1)}/{d2}", "Tæller gange tæller, nævner gange nævner – og forkort til sidst.", AnswerKind.Fraction);
                        }
                    default:
                        {
                            var den = Pick(3, 4, 5, 6, 8);
                            return Expr(Topic.Broeker, d, $"{R(2, 9)} · {R(1, den - 1)}/{den}", "Et helt tal gange en brøk: gang kun tælleren.", AnswerKind.Fraction);
                        }
                }
        }
    }

    private MathTask Percent(int d)
    {
        switch (d)
        {
            case 1:
                {
                    var p = Pick(10, 25, 50, 100);
                    var total = p switch { 25 => 4 * R(5, 60), 50 => 2 * R(10, 200), _ => 10 * R(3, 90) };
                    var res = total * p / 100;
                    return Simple(Topic.Procent, d, $"Hvad er {p} % af {I(total)}?", res, null,
                        p switch { 10 => "10 % er det samme som 1/10: del med 10.", 25 => "25 % er det samme som 1/4: del med 4.", 50 => "50 % er det halve.", _ => "100 % er det hele." },
                        [$"{p} % = {p}/100", $"{p} % af {I(total)} = {I(total)} · {p} : 100 = {I(res)}"]);
                }
            case 2:
                {
                    if (rng.Next(2) == 0)
                    {
                        var p = Pick(5, 15, 20, 30, 40, 60, 75);
                        var total = 20 * R(2, 40);
                        var res = new Rational(total * p, 100);
                        return Simple(Topic.Procent, d, $"Hvad er {p} % af {I(total)}?", res, null,
                            "Find først 1 % (del med 100) eller 10 % (del med 10), og gang op.",
                            [$"10 % af {I(total)} = {F(new Rational(total, 10))}", $"{p} % = {p}/100, så {I(total)} · {p} : 100 = {F(res)}"]);
                    }
                    var price = 50 * R(2, 30);
                    var pct = Pick(10, 20, 25, 30, 50);
                    var discount = price * pct / 100;
                    return Simple(Topic.Procent, d, $"En cykel koster {I(price)} kr. Der er {pct} % rabat. Hvad koster den nu?", price - discount, "kr.",
                        "Find først rabatten i kroner, og træk den fra prisen.",
                        [$"Rabat: {pct} % af {I(price)} = {I(discount)} kr.", $"Ny pris: {I(price)} − {I(discount)} = {I(price - discount)} kr."]);
                }
            default:
                {
                    if (rng.Next(2) == 0)
                    {
                        var total = Pick(20, 25, 40, 50, 80, 200);
                        var p = Pick(10, 20, 30, 40, 60, 75, 80);
                        if (total * p % 100 != 0) p = 20;
                        var part = total * p / 100;
                        return Simple(Topic.Procent, d, $"I en klasse er der {total} elever. {part} af dem cykler i skole. Hvor mange procent er det?", p, "%",
                            "Skriv det som en brøk (del / hele), og lav brøken om til hundrededele.",
                            [$"Brøken er {part}/{total}", $"{part} : {total} = {F(new Rational(part, total))}", $"{F(new Rational(part, total))} = {p}/100 = {p} %"]);
                    }
                    var (n, den) = Pick((1, 4), (3, 4), (1, 5), (2, 5), (3, 5), (1, 2), (7, 10), (3, 20), (9, 25));
                    var pctValue = new Rational(n * 100, den);
                    return Simple(Topic.Procent, d, $"Skriv brøken {n}/{den} som procent.", pctValue, "%",
                        "Forlæng brøken, så nævneren bliver 100. Tælleren er så procenten.",
                        [$"{n}/{den} = {F(pctValue)}/100", $"{n}/{den} = {F(pctValue)} %"], display: $"{n}/{den} = ? %");
                }
        }
    }

    // ------------------------------------------------------------ ligninger

    private MathTask Equations(int d)
    {
        var x = R(2, 15);
        string eq;
        List<string> sol;
        switch (d)
        {
            case 1:
                if (rng.Next(2) == 0)
                {
                    var a = R(3, 30);
                    eq = $"x + {a} = {x + a}";
                    sol = [$"Træk {a} fra på begge sider: x = {x + a} − {a}", $"x = {x}"];
                }
                else
                {
                    var a = R(2, 20);
                    eq = $"x − {a} = {x}";
                    x += a;
                    sol = [$"Læg {a} til på begge sider: x = {x - a} + {a}", $"x = {x}"];
                }
                break;
            case 2:
                if (rng.Next(2) == 0)
                {
                    var a = R(2, 9);
                    eq = $"{a} · x = {a * x}";
                    sol = [$"Del med {a} på begge sider: x = {a * x} : {a}", $"x = {x}"];
                }
                else
                {
                    var a = R(2, 6);
                    eq = $"x : {a} = {x}";
                    x *= a;
                    sol = [$"Gang med {a} på begge sider: x = {x / a} · {a}", $"x = {x}"];
                }
                break;
            default:
                {
                    var a = R(2, 6);
                    var b = R(1, 20);
                    if (rng.Next(2) == 0)
                    {
                        eq = $"{a} · x + {b} = {a * x + b}";
                        sol = [$"Træk {b} fra på begge sider: {a} · x = {a * x}", $"Del med {a} på begge sider: x = {a * x} : {a}", $"x = {x}"];
                    }
                    else
                    {
                        eq = $"{a} · x − {b} = {a * x - b}";
                        sol = [$"Læg {b} til på begge sider: {a} · x = {a * x}", $"Del med {a} på begge sider: x = {a * x} : {a}", $"x = {x}"];
                    }
                    break;
                }
        }
        sol.Add($"Tjek: sæt {x} ind i stedet for x – passer det?");
        return Simple(Topic.Ligninger, d, "Find x:", x, null,
            "En ligning er som en vægt: gør det samme på begge sider af lighedstegnet, indtil x står alene.", sol, display: eq);
    }

    // ------------------------------------------------------------ geometri

    private MathTask Geometry(int d)
    {
        switch (d)
        {
            case 1:
                {
                    var w = R(3, 15);
                    var h = R(2, Math.Max(3, w - 1));
                    var visual = new Visual { Kind = "rect", Values = [w, h], Labels = [$"{w} cm", $"{h} cm"] };
                    if (rng.Next(2) == 0)
                        return Simple(Topic.Geometri, d, "Hvad er rektanglets omkreds?", 2 * (w + h), "cm",
                            "Omkreds er hele vejen rundt: læg alle fire sider sammen.",
                            [$"Omkreds = {w} + {h} + {w} + {h}", $"= 2 · ({w} + {h}) = {2 * (w + h)} cm"], visual: visual);
                    return Simple(Topic.Geometri, d, "Hvad er rektanglets areal?", w * h, "cm²",
                        "Areal af et rektangel = længde · bredde.",
                        [$"Areal = længde · bredde", $"= {w} · {h} = {w * h} cm²"], visual: visual);
                }
            case 2:
                {
                    if (rng.Next(2) == 0)
                    {
                        var g = 2 * R(2, 8);
                        var h = R(3, 12);
                        return Simple(Topic.Geometri, d, "Hvad er trekantens areal?", g * h / 2, "cm²",
                            "Areal af en trekant = ½ · grundlinje · højde.",
                            [$"Areal = ½ · g · h", $"= ½ · {g} · {h}", $"= {g * h} : 2 = {g * h / 2} cm²"],
                            visual: new Visual { Kind = "righttriangle", Values = [g, h], Labels = [$"{g} cm", $"{h} cm"] });
                    }
                    var a = R(30, 100);
                    var b = R(20, 150 - a);
                    var c = 180 - a - b;
                    return Simple(Topic.Geometri, d, "Hvor mange grader er vinkel C?", c, "°",
                        "Vinklerne i en trekant giver altid 180° tilsammen.",
                        [$"A + B + C = 180°", $"C = 180° − {a}° − {b}° = {c}°"],
                        visual: new Visual { Kind = "triangle", Values = [a, b, c], Labels = [$"{a}°", $"{b}°", "?"] });
                }
            default:
                {
                    if (rng.Next(2) == 0)
                    {
                        // L-shape: big rectangle W×H with a cut-out corner cw×ch.
                        var w = R(8, 14);
                        var h = R(6, 10);
                        var cw = R(2, w - 4);
                        var ch = R(2, h - 3);
                        var area = w * h - cw * ch;
                        return Simple(Topic.Geometri, d, "Hvad er arealet af figuren? Alle hjørner er rette vinkler.", area, "cm²",
                            "Tænk på figuren som et stort rektangel, hvor der er skåret et lille rektangel væk.",
                            [$"Stort rektangel: {w} · {h} = {w * h} cm²", $"Bortskåret hjørne: {cw} · {ch} = {cw * ch} cm²", $"Areal = {w * h} − {cw * ch} = {area} cm²"],
                            visual: new Visual { Kind = "lshape", Values = [w, h, cw, ch], Labels = [$"{w} cm", $"{h} cm", $"{cw} cm", $"{ch} cm"] });
                    }
                    var side = R(3, 15);
                    if (rng.Next(2) == 0)
                        return Simple(Topic.Geometri, d, $"Et kvadrat har omkredsen {4 * side} cm. Hvad er kvadratets areal?", side * side, "cm²",
                            "Alle fire sider i et kvadrat er lige lange. Find først én side.",
                            [$"Én side = {4 * side} : 4 = {side} cm", $"Areal = {side} · {side} = {side * side} cm²"],
                            visual: new Visual { Kind = "rect", Values = [1, 1], Labels = ["?", "?"] });
                    var w2 = R(4, 12);
                    var h2 = R(3, 12);
                    return Simple(Topic.Geometri, d, $"Et rektangel har arealet {w2 * h2} cm². Den ene side er {w2} cm. Hvor lang er den anden side?", h2, "cm",
                        "Areal = længde · bredde. Så er den manglende side = areal : den kendte side.",
                        [$"{w2} · ? = {w2 * h2}", $"? = {w2 * h2} : {w2} = {h2} cm"],
                        visual: new Visual { Kind = "rect", Values = [w2, h2], Labels = [$"{w2} cm", "?"] });
                }
        }
    }

    // ------------------------------------------------------------ koordinatsystem

    private static MathTask Point(Topic topic, int difficulty, string prompt, int x, int y, string hint, IReadOnlyList<string> solution, Visual visual) => new()
    {
        Topic = topic,
        Difficulty = difficulty,
        Prompt = prompt,
        Kind = AnswerKind.Point,
        Answer = string.Create(CultureInfo.InvariantCulture, $"{x};{y}"),
        AnswerText = $"({x}, {y})",
        Hint = hint,
        Solution = solution,
        Visual = visual,
    };

    private MathTask Coordinates(int d)
    {
        var (min, max) = d == 1 ? (0, 10) : (-6, 6);
        int RX() => R(min == 0 ? 1 : min, max);
        const string hint = "Først x (hen ad, langs 1. aksen), så y (op eller ned, langs 2. aksen). Start altid i (0, 0).";

        switch (d == 1 ? rng.Next(2) : d == 2 ? rng.Next(2) : 2 + rng.Next(2))
        {
            case 0:
                {
                    int x, y;
                    do { x = RX(); y = RX(); } while (d > 1 && (x == 0 || y == 0));
                    var others = Enumerable.Range(0, 2).Select(i => new GridPoint(((char)('B' + i)).ToString(), RX(), RX())).Where(p => p.X != x || p.Y != y).ToList();
                    return Point(Topic.Koordinatsystem, d, "Hvad er koordinaterne til punkt A?", x, y, hint,
                        [$"Gå fra (0, 0) hen ad x-aksen til {x}.", $"Gå {(y >= 0 ? "op" : "ned")} til {y}.", $"A = ({x}, {y})"],
                        new Visual { Kind = "grid", Min = min, Max = max, Points = [new("A", x, y), .. others] });
                }
            case 1:
                {
                    int x, y;
                    do { x = RX(); y = RX(); } while (d > 1 && (x == 0 || y == 0));
                    return Point(Topic.Koordinatsystem, d, $"Klik i koordinatsystemet og sæt punktet ({x}, {y}).", x, y, hint,
                        [$"Gå fra (0, 0) hen ad x-aksen til {x}.", $"Gå {(y >= 0 ? "op" : "ned")} til {y}.", $"Punktet ligger i ({x}, {y})"],
                        new Visual { Kind = "grid", Min = min, Max = max, Clickable = true });
                }
            case 2:
                {
                    // Square/rectangle ABCD: find D.
                    var x1 = R(-5, 1);
                    var y1 = R(-5, 1);
                    var w = R(2, 4);
                    var h = R(2, 4);
                    var pts = new[] { new GridPoint("A", x1, y1), new GridPoint("B", x1 + w, y1), new GridPoint("C", x1 + w, y1 + h) };
                    return Point(Topic.Koordinatsystem, d, $"A({x1}, {y1}), B({x1 + w}, {y1}) og C({x1 + w}, {y1 + h}) er tre hjørner i rektanglet ABCD. Klik, hvor hjørnet D skal være.",
                        x1, y1 + h, "Tegn rektanglet. D ligger lige over A og lige til venstre for C.",
                        [$"D har samme x som A: {x1}", $"D har samme y som C: {y1 + h}", $"D = ({x1}, {y1 + h})"],
                        new Visual { Kind = "grid", Min = min, Max = max, Points = [.. pts], Clickable = true });
                }
            default:
                {
                    // Mirror a point in an axis.
                    int x, y;
                    do { x = R(-6, 6); y = R(-6, 6); } while (x == 0 || y == 0);
                    var inX = rng.Next(2) == 0;
                    var (mx, my) = inX ? (x, -y) : (-x, y);
                    return Point(Topic.Koordinatsystem, d, $"Spejl punktet P({x}, {y}) i {(inX ? "x-aksen" : "y-aksen")}. Klik, hvor spejlbilledet havner.",
                        mx, my, inX ? "Spejler du i x-aksen, beholder punktet sin x-værdi, men y skifter fortegn." : "Spejler du i y-aksen, beholder punktet sin y-værdi, men x skifter fortegn.",
                        [inX ? $"x bliver ved med at være {x}" : $"x skifter fortegn: {x} → {mx}", inX ? $"y skifter fortegn: {y} → {my}" : $"y bliver ved med at være {y}", $"Spejlbilledet er ({mx}, {my})"],
                        new Visual { Kind = "grid", Min = -6, Max = 6, Points = [new("P", x, y)], Clickable = true });
                }
        }
    }
}
