using System.Diagnostics.CodeAnalysis;

namespace RetroTink.Profiles;

/// <summary>
/// Stable identifier of a setting, independent from menu positions and from any binary layout.
/// </summary>
/// <remarks>
/// Form: dot-separated segments (for example <c>output.resolution</c> or <c>geometry.pictureFormat</c>); each
/// segment starts with an ASCII letter followed by ASCII letters or digits. Total length 1 to 64 characters.
/// Comparison is ordinal (case-sensitive).
/// </remarks>
public sealed class SettingId : IEquatable<SettingId>
{
    /// <summary>Maximum length of an identifier.</summary>
    public const int MaxLength = 64;

    private SettingId(string value) => Value = value;

    /// <summary>Textual value.</summary>
    public string Value { get; }

    /// <summary>Creates an identifier or throws when <paramref name="value"/> is not valid.</summary>
    /// <exception cref="ArgumentException">The value is not a valid setting identifier.</exception>
    public static SettingId From(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return TryFrom(value, out var id)
            ? id
            : throw new ArgumentException($"'{value}' is not a valid setting id: expected dot-separated segments of ASCII letters and digits, each starting with a letter, up to {MaxLength} characters.", nameof(value));
    }

    /// <summary>Attempts to create an identifier from untrusted text.</summary>
    public static bool TryFrom(string? value, [MaybeNullWhen(false)] out SettingId id)
    {
        id = null;
        if (value is null || value.Length == 0 || value.Length > MaxLength)
        {
            return false;
        }

        var segmentStart = true;
        foreach (var c in value)
        {
            if (segmentStart)
            {
                if (!Identifiers.IsAsciiLetter(c))
                {
                    return false;
                }

                segmentStart = false;
            }
            else if (c == '.')
            {
                segmentStart = true;
            }
            else if (!Identifiers.IsAsciiLetterOrDigit(c))
            {
                return false;
            }
        }

        if (segmentStart)
        {
            return false; // trailing dot
        }

        id = new SettingId(value);
        return true;
    }

    /// <inheritdoc />
    public bool Equals(SettingId? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SettingId);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Value equality.</summary>
    public static bool operator ==(SettingId? left, SettingId? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(SettingId? left, SettingId? right) => !(left == right);
}
