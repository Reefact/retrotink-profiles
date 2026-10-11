using System.Diagnostics.CodeAnalysis;

namespace RetroTink.Profiles;

/// <summary>Human readable name of a profile. Carries no identity: two profiles may share a name.</summary>
/// <remarks>
/// Leading and trailing white space is trimmed. The result must be 1 to 128 characters long and must not
/// contain control characters. Any other Unicode text is accepted. Comparison is ordinal (case-sensitive).
/// </remarks>
public sealed class ProfileName : IEquatable<ProfileName>
{
    /// <summary>Maximum length of a name, after trimming.</summary>
    public const int MaxLength = 128;

    private ProfileName(string value) => Value = value;

    /// <summary>Trimmed textual value.</summary>
    public string Value { get; }

    /// <summary>Creates a name or throws when <paramref name="value"/> is not acceptable.</summary>
    /// <exception cref="ArgumentException">The value is empty, too long or contains control characters.</exception>
    public static ProfileName From(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return TryFrom(value, out var name)
            ? name
            : throw new ArgumentException($"'{value}' is not a valid profile name: expected 1 to {MaxLength} characters without control characters.", nameof(value));
    }

    /// <summary>Attempts to create a name from untrusted text.</summary>
    public static bool TryFrom(string? value, [MaybeNullWhen(false)] out ProfileName name)
    {
        name = null;
        if (value is null)
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0 || trimmed.Length > MaxLength || Identifiers.ContainsControlCharacter(trimmed))
        {
            return false;
        }

        name = new ProfileName(trimmed);
        return true;
    }

    /// <inheritdoc />
    public bool Equals(ProfileName? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ProfileName);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Value equality.</summary>
    public static bool operator ==(ProfileName? left, ProfileName? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(ProfileName? left, ProfileName? right) => !(left == right);
}
