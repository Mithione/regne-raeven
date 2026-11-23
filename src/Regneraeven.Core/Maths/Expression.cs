using System.Globalization;
using System.Numerics;
using System.Text;

namespace Regneraeven.Core.Maths;

/// <summary>How a number is shown: whole number, decimal number (2,5) or fraction (3/4).</summary>
public enum NumForm { Integer, Decimal, Fraction }

/// <summary>Arithmetic expression tree. Kids write ":" or "/" for division, "·", "*", "x" or "×" for times.</summary>
public abstract record Node;

/// <summary>A finished number.</summary>
public sealed record Num(Rational Value, NumForm Form) : Node;

/// <summary>A fraction written by the user, e.g. 6/8 – kept as written until it is shortened.</summary>
public sealed record FracLit(BigInteger N, BigInteger D) : Node
{
    public bool IsReduced => !D.IsOne && BigInteger.GreatestCommonDivisor(N, D).IsOne && D.Sign > 0;
}

public sealed record Bin(char Op, Node Left, Node Right) : Node;
public sealed record Group(Node Inner) : Node;
public sealed record Neg(Node Inner) : Node;

public sealed record SolveStep(string Expression, string Explanation);

public sealed record SolveResult(string Input, IReadOnlyList<SolveStep> Steps, Rational Value, NumForm Form)
{
    /// <summary>The answer as kids would write it: 1 1/4, 2,5 or 12.</summary>
    public string ResultText => Expression.FormatValue(Value, Form, mixed: true);
}

/// <summary>
/// Parses school arithmetic and solves it one operation at a time, following the Danish
/// rule "parenteser først, så gange og division, til sidst plus og minus – fra venstre mod højre".
/// </summary>
public static class Expression
{
    public static bool TryParse(string? text, out Node node, out string error)
    {
        node = null!;
        error = "";
        if (string.IsNullOrWhiteSpace(text)) { error = "Skriv et regnestykke."; return false; }
        try
        {
            var p = new Parser(text);
            node = p.ParseAll();
            return true;
        }
        catch (FormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Solves the expression step by step. Throws <see cref="FormatException"/> if it can't be read.</summary>
    public static SolveResult Solve(string text)
    {
        if (!TryParse(text, out var node, out var error)) throw new FormatException(error);
        return Solve(node, Render(node));
    }

    public static SolveResult Solve(Node node, string input)
    {
        var steps = new List<SolveStep>();
        node = Normalize(node);
        var guard = 0;
        while (node is not Num)
        {
            if (++guard > 200) throw new FormatException("Regnestykket er for langt.");
            var hasAddSub = ContainsAddSub(node);
            var (next, explanation) = StepOnce(node, hasAddSub);
            node = Normalize(next);
            steps.Add(new SolveStep(Render(node), explanation));
        }
        var num = (Num)node;
        return new SolveResult(input, steps, num.Value, num.Form);
    }

    public static Rational Evaluate(Node node) => node switch
    {
        Num n => n.Value,
        FracLit f => new Rational(f.N, f.D),
        Group g => Evaluate(g.Inner),
        Neg n => -Evaluate(n.Inner),
        Bin b => Apply(b.Op, Evaluate(b.Left), Evaluate(b.Right)),
        _ => throw new InvalidOperationException(),
    };

    private static Rational Apply(char op, Rational a, Rational b) => op switch
    {
        '+' => a + b,
        '-' => a - b,
        '*' => a * b,
        '/' => a / b,
        _ => throw new InvalidOperationException(),
    };

    // ---------------------------------------------------------------- rendering

    public static string FormatValue(Rational value, NumForm form, bool mixed = false)
    {
        if (value.IsInteger) return Rational.FormatInteger(value.Numerator);
        if (form == NumForm.Decimal && value.HasFiniteDecimals) return value.ToDecimalString();
        if (form == NumForm.Decimal) return "≈ " + value.ToDecimalString(3);
        return mixed ? value.ToMixedString() : value.ToFractionString();
    }

    public static string Render(Node node) => Render(node, rightOperand: false);

    private static string Render(Node node, bool rightOperand) => node switch
    {
        Num n when n.Value.Sign < 0 && rightOperand => "(" + FormatValue(n.Value, n.Form) + ")",
        Num n => FormatValue(n.Value, n.Form),
        FracLit f => $"{Rational.FormatInteger(f.N)}/{Rational.FormatInteger(f.D)}",
        Group g => "(" + Render(g.Inner) + ")",
        Neg n => rightOperand ? "(-" + Render(n.Inner, true) + ")" : "-" + Render(n.Inner, true),
        Bin b => Render(b.Left, false) + " " + Symbol(b.Op) + " " + Render(b.Right, true),
        _ => "?",
    };

    private static string Symbol(char op) => op switch { '+' => "+", '-' => "−", '*' => "·", '/' => ":", _ => op.ToString() };

    // ---------------------------------------------------------------- stepping

    private static bool IsTerminal(Node n) => n is Num || n is FracLit { IsReduced: true };

    private static (Rational Value, NumForm Form) ValueOf(Node n) => n switch
    {
        Num x => (x.Value, x.Form),
        FracLit f => (new Rational(f.N, f.D), NumForm.Fraction),
        _ => throw new InvalidOperationException(),
    };

    /// <summary>Removes parentheses around a single number and folds "-number".</summary>
    private static Node Normalize(Node node) => node switch
    {
        Group g => Normalize(g.Inner) is var inner && IsTerminal(inner) ? AsNum(inner) : new Group(Normalize(g.Inner)),
        Neg n => Normalize(n.Inner) is var inner && IsTerminal(inner)
            ? ValueOf(inner) is var (v, f) ? new Num(-v, f) : inner
            : new Neg(Normalize(n.Inner)),
        Bin b => new Bin(b.Op, Normalize(b.Left), Normalize(b.Right)),
        FracLit { D.IsOne: true } f => new Num(new Rational(f.N), NumForm.Integer),
        _ => node,
    };

    private static Node AsNum(Node n)
    {
        var (v, f) = ValueOf(n);
        return new Num(v, v.IsInteger ? NumForm.Integer : f);
    }

    private static bool ContainsAddSub(Node node) => node switch
    {
        Bin { Op: '+' or '-' } => true,
        Bin b => ContainsAddSub(b.Left) || ContainsAddSub(b.Right),
        Group g => ContainsAddSub(g.Inner),
        Neg n => ContainsAddSub(n.Inner),
        _ => false,
    };

    private sealed record Candidate(Node Target, int Depth, bool MulDiv, int Order);

    private static void Collect(Node node, int depth, List<Candidate> list, ref int order)
    {
        switch (node)
        {
            case FracLit f when !f.IsReduced:
                list.Add(new Candidate(f, depth, true, order++));
                break;
            case Group g:
                Collect(g.Inner, depth + 1, list, ref order);
                break;
            case Neg n:
                Collect(n.Inner, depth, list, ref order);
                break;
            case Bin b:
                Collect(b.Left, depth, list, ref order);
                if (IsTerminal(b.Left) && IsTerminal(b.Right))
                    list.Add(new Candidate(b, depth, b.Op is '*' or '/', order++));
                Collect(b.Right, depth, list, ref order);
                break;
        }
    }

    private static (Node, string) StepOnce(Node root, bool hasAddSub)
    {
        var candidates = new List<Candidate>();
        var order = 0;
        Collect(root, 0, candidates, ref order);
        if (candidates.Count == 0) throw new FormatException("Regnestykket kunne ikke regnes ud.");

        var pick = candidates
            .OrderByDescending(c => c.Depth)
            .ThenByDescending(c => c.MulDiv)
            .ThenBy(c => c.Order)
            .First();

        var (replacement, detail) = Reduce(pick.Target);
        var reason = pick.Target is FracLit
            ? "Forkort brøken"
            : pick.Depth > 0 ? "Først det inde i parentesen"
            : pick.MulDiv && hasAddSub ? "Gange og division før plus og minus"
            : "Fra venstre mod højre";
        // With only one operation left there is no order to explain.
        var text = CountOperations(root) <= 1 && pick.Target is Bin
            ? char.ToUpperInvariant(detail[0]) + detail[1..]
            : $"{reason}: {detail}";
        return (Replace(root, pick.Target, replacement), text);
    }

    private static int CountOperations(Node node) => node switch
    {
        Bin b => 1 + CountOperations(b.Left) + CountOperations(b.Right),
        Group g => CountOperations(g.Inner),
        Neg n => CountOperations(n.Inner),
        FracLit f when !f.IsReduced => 1,
        _ => 0,
    };

    private static Node Replace(Node node, Node target, Node replacement)
    {
        if (ReferenceEquals(node, target)) return replacement;
        return node switch
        {
            Group g => new Group(Replace(g.Inner, target, replacement)),
            Neg n => new Neg(Replace(n.Inner, target, replacement)),
            Bin b => new Bin(b.Op, Replace(b.Left, target, replacement), Replace(b.Right, target, replacement)),
            _ => node,
        };
    }

    private static (Node Result, string Detail) Reduce(Node target)
    {
        if (target is FracLit f)
        {
            var v = new Rational(f.N, f.D);
            var shortened = v.IsInteger ? Rational.FormatInteger(v.Numerator) : v.ToFractionString();
            var gcd = BigInteger.GreatestCommonDivisor(f.N, f.D);
            var how = v.IsInteger ? $"{Rational.FormatInteger(f.N)} : {Rational.FormatInteger(f.D)} = {shortened}" : $"del tæller og nævner med {gcd}: {Render(f)} = {shortened}";
            return (new Num(v, v.IsInteger ? NumForm.Integer : NumForm.Fraction), how);
        }

        var b = (Bin)target;
        var (a, af) = ValueOf(b.Left);
        var (c, cf) = ValueOf(b.Right);
        if (b.Op == '/' && c.Sign == 0) throw new FormatException("Man kan ikke dividere med 0.");
        var result = Apply(b.Op, a, c);

        var form = af == NumForm.Decimal || cf == NumForm.Decimal ? NumForm.Decimal
            : af == NumForm.Fraction || cf == NumForm.Fraction || !result.IsInteger ? NumForm.Fraction
            : NumForm.Integer;
        if (result.IsInteger) form = NumForm.Integer;
        if (form == NumForm.Decimal && !result.HasFiniteDecimals) form = NumForm.Fraction;

        var left = Render(b.Left);
        var right = Render(b.Right, true);
        var shown = FormatValue(result, form);
        var plain = $"{left} {Symbol(b.Op)} {right} = {shown}";

        var bothFractions = af == NumForm.Fraction || cf == NumForm.Fraction;
        if (bothFractions && form != NumForm.Decimal)
        {
            if (b.Op is '+' or '-')
            {
                var common = Lcm(a.Denominator, c.Denominator);
                if (a.Denominator != c.Denominator)
                {
                    var a2 = $"{Rational.FormatInteger(a.Numerator * (common / a.Denominator))}/{Rational.FormatInteger(common)}";
                    var c2 = $"{Rational.FormatInteger(c.Numerator * (common / c.Denominator))}/{Rational.FormatInteger(common)}";
                    var raw = a.Numerator * (common / a.Denominator) + (b.Op == '+' ? 1 : -1) * c.Numerator * (common / c.Denominator);
                    var rawText = $"{Rational.FormatInteger(raw)}/{Rational.FormatInteger(common)}";
                    var tail = rawText == shown ? "" : $" = {shown}";
                    return (new Num(result, form), $"find fællesnævneren {Rational.FormatInteger(common)}. {left} {Symbol(b.Op)} {right} = {a2} {Symbol(b.Op)} {c2} = {rawText}{tail}");
                }
                return (new Num(result, form), $"samme nævner – regn med tællerne. {plain}");
            }
            if (b.Op == '*')
            {
                var rawN = a.Numerator * c.Numerator;
                var rawD = a.Denominator * c.Denominator;
                var raw = rawD.IsOne ? Rational.FormatInteger(rawN) : $"{Rational.FormatInteger(rawN)}/{Rational.FormatInteger(rawD)}";
                var tail = raw == shown ? "" : $" = {shown}";
                return (new Num(result, form), $"tæller gange tæller og nævner gange nævner. {left} · {right} = {raw}{tail}");
            }
            if (b.Op == '/' && cf == NumForm.Fraction)
            {
                var flipped = new Rational(c.Denominator, c.Numerator);
                return (new Num(result, form), $"at dividere med en brøk er det samme som at gange med den omvendte brøk. {left} : {right} = {left} · {FormatValue(flipped, NumForm.Fraction)} = {shown}");
            }
        }

        if (b.Op == '/' && form == NumForm.Fraction && af == NumForm.Integer && cf == NumForm.Integer)
        {
            var whole = BigInteger.Divide(a.Numerator, c.Numerator);
            var rest = a.Numerator - whole * c.Numerator;
            var restText = !whole.IsZero && rest.Sign > 0 && c.Sign > 0 ? $" (det er {Rational.FormatInteger(whole)} med {Rational.FormatInteger(rest)} til rest)" : "";
            return (new Num(result, form), $"{plain}{(result.ToMixedString() != shown ? " = " + result.ToMixedString() : "")}{restText}");
        }

        return (new Num(result, form), plain);
    }

    private static BigInteger Lcm(BigInteger a, BigInteger b) => a / BigInteger.GreatestCommonDivisor(a, b) * b;

    // ---------------------------------------------------------------- similar example

    /// <summary>
    /// Makes an example with the same structure but other numbers, so the helper can show the method
    /// without giving away the answer to the child's own task.
    /// </summary>
    public static Node MakeSimilar(Node node, Random rng)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = Similar(node, rng);
            try
            {
                Evaluate(candidate);
                if (Render(candidate) != Render(node)) return candidate;
            }
            catch (DivideByZeroException) { }
        }
        return node;
    }

    private static Node Similar(Node node, Random rng) => node switch
    {
        Num n => new Num(SimilarNumber(n.Value, n.Form, rng), n.Form),
        FracLit f => SimilarFraction(f, rng),
        Group g => new Group(Similar(g.Inner, rng)),
        Neg n => new Neg(Similar(n.Inner, rng)),
        Bin { Op: '/', Left: Num { Form: NumForm.Integer } l, Right: Num { Form: NumForm.Integer } r } b
            when !r.Value.Numerator.IsZero && (l.Value / r.Value).IsInteger => DivisibleDivision(l.Value, r.Value, rng),
        Bin b => new Bin(b.Op, Similar(b.Left, rng), Similar(b.Right, rng)),
        _ => node,
    };

    private static Node DivisibleDivision(Rational dividend, Rational divisor, Random rng)
    {
        var d = SimilarNumber(divisor, NumForm.Integer, rng);
        if (d.Sign == 0) d = 2;
        var quotient = SimilarNumber(dividend / divisor, NumForm.Integer, rng);
        return new Bin('/', new Num(quotient * d, NumForm.Integer), new Num(d, NumForm.Integer));
    }

    private static Node SimilarFraction(FracLit f, Random rng)
    {
        var d = (int)BigInteger.Min(f.D, 100);
        var proper = BigInteger.Abs(f.N) < f.D;
        int n;
        do n = proper ? rng.Next(1, Math.Max(2, d)) : rng.Next(d + 1, d * 3);
        while (d > 2 && BigInteger.GreatestCommonDivisor(n, d) != BigInteger.GreatestCommonDivisor(f.N, f.D) && rng.Next(4) != 0);
        return new FracLit(f.N.Sign < 0 ? -n : n, f.D);
    }

    private static Rational SimilarNumber(Rational value, NumForm form, Random rng)
    {
        if (form == NumForm.Decimal && !value.IsInteger)
        {
            var text = value.ToDecimalString();
            var decimals = text.Contains(',') ? text.Length - text.IndexOf(',') - 1 : 0;
            var scale = (int)Math.Pow(10, decimals);
            var scaled = (value * scale).Numerator;
            var newScaled = SimilarInteger(scaled, rng);
            if (newScaled % 10 == 0) newScaled += 1;
            return new Rational(newScaled, scale);
        }
        if (!value.IsInteger) return value;
        return SimilarInteger(value.Numerator, rng);
    }

    private static BigInteger SimilarInteger(BigInteger value, Random rng)
    {
        var abs = BigInteger.Abs(value);
        if (abs.IsZero) return 0;
        if (abs > 1_000_000) return value;
        var digits = abs.ToString(CultureInfo.InvariantCulture).Length;
        var min = digits == 1 ? 2 : (int)Math.Pow(10, digits - 1);
        var max = (int)Math.Pow(10, digits) - 1;
        if (abs == 1) return value;
        var n = rng.Next(min, max + 1);
        return value.Sign < 0 ? -n : n;
    }

    // ---------------------------------------------------------------- parser

    private sealed class Parser(string text)
    {
        private readonly List<(char Kind, string Text)> _tokens = Tokenize(text);
        private int _pos;

        public Node ParseAll()
        {
            if (_tokens.Count == 0) throw new FormatException("Skriv et regnestykke.");
            var node = ParseAdd();
            if (_pos < _tokens.Count) throw new FormatException($"Jeg forstår ikke »{_tokens[_pos].Text}« her.");
            return node;
        }

        private (char Kind, string Text)? Peek => _pos < _tokens.Count ? _tokens[_pos] : null;

        private Node ParseAdd()
        {
            var left = ParseMul();
            while (Peek is { Kind: '+' or '-' } t)
            {
                _pos++;
                left = new Bin(t.Kind, left, ParseMul());
            }
            return left;
        }

        private Node ParseMul()
        {
            var left = ParseUnary();
            while (Peek is { Kind: '*' or '/' or ':' } t)
            {
                _pos++;
                var right = ParseUnary();
                left = new Bin(t.Kind == ':' ? '/' : t.Kind, left, right);
            }
            return left;
        }

        private Node ParseUnary()
        {
            if (Peek is { Kind: '-' }) { _pos++; return new Neg(ParseUnary()); }
            if (Peek is { Kind: '+' }) { _pos++; return ParseUnary(); }
            return ParsePrimary();
        }

        private Node ParsePrimary()
        {
            var t = Peek ?? throw new FormatException("Regnestykket slutter for tidligt.");
            if (t.Kind == '(')
            {
                _pos++;
                var inner = ParseAdd();
                if (Peek is not { Kind: ')' }) throw new FormatException("Der mangler en slut-parentes »)«.");
                _pos++;
                return new Group(inner);
            }
            if (t.Kind != 'n') throw new FormatException($"Jeg forstår ikke »{t.Text}« her.");
            _pos++;
            var (value, isDecimal) = ParseNumber(t.Text);

            // 3/4 written with "/" between two whole numbers is a fraction.
            if (!isDecimal && Peek is { Kind: '/' } && _pos + 1 < _tokens.Count && _tokens[_pos + 1].Kind == 'n'
                && !_tokens[_pos + 1].Text.Contains(','))
            {
                var (den, _) = ParseNumber(_tokens[_pos + 1].Text);
                if (den.Sign == 0) throw new FormatException("En brøk kan ikke have 0 i nævneren.");
                _pos += 2;
                return new FracLit(value.Numerator, den.Numerator);
            }
            return new Num(value, isDecimal ? NumForm.Decimal : NumForm.Integer);
        }

        private static (Rational, bool) ParseNumber(string s)
        {
            var isDecimal = s.Contains(',');
            var d = decimal.Parse(s.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            return (Rational.FromDecimal(d), isDecimal);
        }

        private static List<(char, string)> Tokenize(string text)
        {
            var tokens = new List<(char, string)>();
            var i = 0;
            text = text.Trim().TrimEnd('=').Trim();
            while (i < text.Length)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (char.IsDigit(c) || (c is ',' or '.' && i + 1 < text.Length && char.IsDigit(text[i + 1])))
                {
                    var sb = new StringBuilder();
                    while (i < text.Length && (char.IsDigit(text[i]) || text[i] is ',' or '.'))
                    {
                        sb.Append(text[i]);
                        i++;
                    }
                    tokens.Add(('n', NormalizeNumber(sb.ToString())));
                    continue;
                }
                char? kind = c switch
                {
                    '+' => '+',
                    '-' or '−' or '–' => '-',
                    '*' or '·' or '×' or '⋅' or 'x' or 'X' => '*',
                    '/' or '÷' => '/',
                    ':' => ':',
                    '(' or '[' => '(',
                    ')' or ']' => ')',
                    _ => null,
                };
                if (kind is null)
                    throw new FormatException(char.IsLetter(c)
                        ? "Hjælperen kan kun regne med tal – ikke bogstaver eller tekst."
                        : $"Jeg kender ikke tegnet »{c}«.");
                tokens.Add((kind.Value, c.ToString()));
                i++;
            }
            return tokens;
        }

        /// <summary>"1.500" is fifteen hundred, "2.5" and "2,5" are two and a half. Result uses "," as decimal mark.</summary>
        private static string NormalizeNumber(string s)
        {
            if (s.Contains(',') && s.Contains('.')) s = s.Replace(".", "");
            else if (s.Contains('.'))
            {
                var parts = s.Split('.');
                var thousands = parts.Length > 1 && parts[0].Length is > 0 and <= 3 && parts[0] != "0" && parts.Skip(1).All(p => p.Length == 3);
                s = thousands ? s.Replace(".", "") : s.Replace('.', ',');
            }
            if (s.Count(ch => ch == ',') > 1) throw new FormatException($"Tallet »{s}« har for mange kommaer.");
            if (s.StartsWith(',')) s = "0" + s;
            if (s.EndsWith(',')) s = s.TrimEnd(',');
            return s;
        }
    }
}
