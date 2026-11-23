using System.Globalization;
using System.Numerics;

namespace Regneraeven.Core.Maths;

/// <summary>
/// An exact fraction (always reduced, denominator positive). Used for all task answers and the
/// step-by-step helper so 0,1 + 0,2 is exactly 0,3 and 1/3 stays 1/3.
/// </summary>
public readonly struct Rational : IEquatable<Rational>, IComparable<Rational>
{
    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }

    public static readonly Rational Zero = new(0);
    public static readonly Rational One = new(1);

    public Rational(BigInteger value) : this(value, BigInteger.One) { }

    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero) throw new DivideByZeroException("Man kan ikke dividere med 0.");
        if (denominator.Sign < 0) { numerator = -numerator; denominator = -denominator; }
        var gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
        if (!gcd.IsZero && !gcd.IsOne) { numerator /= gcd; denominator /= gcd; }
        Numerator = numerator;
        Denominator = numerator.IsZero ? BigInteger.One : denominator;
    }

    public bool IsInteger => Denominator.IsOne;
    public int Sign => Numerator.Sign;

    /// <summary>True when the value can be written with a finite number of decimals (denominator has only 2s and 5s).</summary>
    public bool HasFiniteDecimals
    {
        get
        {
            var d = Denominator;
            while ((d % 2).IsZero) d /= 2;
            while ((d % 5).IsZero) d /= 5;
            return d.IsOne;
        }
    }

    public static Rational FromDecimal(decimal value)
    {
        var bits = decimal.GetBits(value);
        var scale = (bits[3] >> 16) & 0xFF;
        var mantissa = new BigInteger((uint)bits[0]) | (new BigInteger((uint)bits[1]) << 32) | (new BigInteger((uint)bits[2]) << 64);
        if (bits[3] < 0) mantissa = -mantissa;
        return new Rational(mantissa, BigInteger.Pow(10, scale));
    }

    public static implicit operator Rational(int value) => new(value);
    public static implicit operator Rational(long value) => new(value);
    public static implicit operator Rational(BigInteger value) => new(value);

    public static Rational operator +(Rational a, Rational b) => new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
    public static Rational operator -(Rational a, Rational b) => new(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);
    public static Rational operator *(Rational a, Rational b) => new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
    public static Rational operator /(Rational a, Rational b)
    {
        if (b.Numerator.IsZero) throw new DivideByZeroException("Man kan ikke dividere med 0.");
        return new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
    }
    public static Rational operator -(Rational a) => new(-a.Numerator, a.Denominator);

    public static bool operator ==(Rational a, Rational b) => a.Equals(b);
    public static bool operator !=(Rational a, Rational b) => !a.Equals(b);
    public static bool operator <(Rational a, Rational b) => a.CompareTo(b) < 0;
    public static bool operator >(Rational a, Rational b) => a.CompareTo(b) > 0;
    public static bool operator <=(Rational a, Rational b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Rational a, Rational b) => a.CompareTo(b) >= 0;

    public Rational Abs() => Sign < 0 ? -this : this;

    public bool Equals(Rational other) => Numerator == other.Numerator && Denominator == other.Denominator;
    public override bool Equals(object? obj) => obj is Rational r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);
    public int CompareTo(Rational other) => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);

    public double ToDouble() => (double)Numerator / (double)Denominator;

    /// <summary>Danish decimal notation with comma, rounded to at most <paramref name="maxDecimals"/> decimals: 2,75.</summary>
    public string ToDecimalString(int maxDecimals = 6)
    {
        var abs = Abs();
        var scale = BigInteger.Pow(10, maxDecimals);
        var scaled = abs.Numerator * scale;
        var q = BigInteger.Divide(scaled, abs.Denominator);
        var r = scaled - q * abs.Denominator;
        if (r * 2 >= abs.Denominator) q += 1;
        var whole = BigInteger.Divide(q, scale);
        var frac = (q - whole * scale).ToString(CultureInfo.InvariantCulture).PadLeft(maxDecimals, '0').TrimEnd('0');
        var text = FormatInteger(whole) + (frac.Length > 0 ? "," + frac : "");
        return Sign < 0 && text != "0" ? "-" + text : text;
    }

    /// <summary>"3/4", "-5/2" or "7".</summary>
    public string ToFractionString() => IsInteger ? FormatInteger(Numerator) : $"{FormatInteger(Numerator)}/{FormatInteger(Denominator)}";

    /// <summary>Mixed number: 7/4 → "1 3/4". Proper fractions and integers are unchanged.</summary>
    public string ToMixedString()
    {
        if (IsInteger || BigInteger.Abs(Numerator) < Denominator) return ToFractionString();
        var whole = BigInteger.Divide(BigInteger.Abs(Numerator), Denominator);
        var rest = BigInteger.Abs(Numerator) - whole * Denominator;
        return (Sign < 0 ? "-" : "") + $"{FormatInteger(whole)} {FormatInteger(rest)}/{FormatInteger(Denominator)}";
    }

    public override string ToString() => HasFiniteDecimals ? ToDecimalString() : ToFractionString();

    /// <summary>Large numbers get a thousands dot like in Danish school books (12.500); small ones don't (1250).</summary>
    public static string FormatInteger(BigInteger value)
    {
        var abs = BigInteger.Abs(value);
        var s = abs.ToString(CultureInfo.InvariantCulture);
        if (abs >= 10000)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < s.Length; i++)
            {
                if (i > 0 && (s.Length - i) % 3 == 0) sb.Append('.');
                sb.Append(s[i]);
            }
            s = sb.ToString();
        }
        return value.Sign < 0 ? "-" + s : s;
    }
}
