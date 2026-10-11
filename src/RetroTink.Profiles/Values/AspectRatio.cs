using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace RetroTink.Profiles.Values;

/// <summary>Display aspect ratio as an exact, reduced fraction.</summary>
/// <remarks>
/// Values are normalised: <c>From(235, 100)</c> and <c>From(47, 20)</c> are the same value and both display as
/// <c>47:20</c>. Exact arithmetic means 2.35:1 and 2.39:1 are never confused.
/// </remarks>
public sealed class AspectRatio : IEquatable<AspectRatio>
{
    private AspectRatio(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>4:3.</summary>
    public static AspectRatio FourThree { get; } = From(4, 3);

    /// <summary>16:9.</summary>
    public static AspectRatio SixteenNine { get; } = From(16, 9);

    /// <summary>Reduced numerator (width term), strictly positive.</summary>
    public int Numerator { get; }

    /// <summary>Reduced denominator (height term), strictly positive.</summary>
    public int Denominator { get; }

    /// <summary>Approximate decimal value (width divided by height), for display only.</summary>
    public double Value => (double)Numerator / Denominator;

    /// <summary>Creates a ratio from strictly positive terms; the result is reduced.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A term is zero or negative.</exception>
    public static AspectRatio From(int numerator, int denominator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numerator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(denominator);
        var gcd = GreatestCommonDivisor(numerator, denominator);
        return new AspectRatio(numerator / gcd, denominator / gcd);
    }

    /// <summary>Attempts to create a ratio from untrusted terms.</summary>
    public static bool TryFrom(int numerator, int denominator, [MaybeNullWhen(false)] out AspectRatio ratio)
    {
        if (numerator <= 0 || denominator <= 0)
        {
            ratio = null;
            return false;
        }

        ratio = From(numerator, denominator);
        return true;
    }

    /// <summary>Returns whether this ratio is wider than <paramref name="other"/>.</summary>
    public bool IsWiderThan(AspectRatio other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return (long)Numerator * other.Denominator > (long)other.Numerator * Denominator;
    }

    /// <inheritdoc />
    public bool Equals(AspectRatio? other) => other is not null && Numerator == other.Numerator && Denominator == other.Denominator;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as AspectRatio);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Numerator}:{Denominator}");

    /// <summary>Value equality.</summary>
    public static bool operator ==(AspectRatio? left, AspectRatio? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(AspectRatio? left, AspectRatio? right) => !(left == right);

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }
}
