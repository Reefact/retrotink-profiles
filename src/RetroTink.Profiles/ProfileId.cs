using System.Diagnostics.CodeAnalysis;

namespace RetroTink.Profiles;

/// <summary>
/// Stable identity of a <see cref="ProfileDefinition"/>, used by other definitions to reference their sources.
/// </summary>
/// <remarks>
/// Canonical form: 1 to 64 ASCII characters among lowercase letters, digits, <c>-</c>, <c>_</c> and <c>.</c>,
/// starting and ending with a letter or digit. Input is case-insensitive and normalised to lowercase, so
/// <c>LD-PAL</c> and <c>ld-pal</c> denote the same profile. An identifier is not a display name: see <see cref="ProfileName"/>.
/// </remarks>
public sealed class ProfileId : IEquatable<ProfileId>
{
    /// <summary>Maximum length of an identifier.</summary>
    public const int MaxLength = 64;

    private ProfileId(string value) => Value = value;

    /// <summary>Canonical (lowercase) textual value.</summary>
    public string Value { get; }

    /// <summary>Creates an identifier or throws when <paramref name="value"/> is not a valid identifier.</summary>
    /// <exception cref="ArgumentException">The value is not a valid profile identifier.</exception>
    public static ProfileId From(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return TryFrom(value, out var id)
            ? id
            : throw new ArgumentException($"'{value}' is not a valid profile id: expected 1 to {MaxLength} ASCII letters, digits, '-', '_' or '.', starting and ending with a letter or digit.", nameof(value));
    }

    /// <summary>Attempts to create an identifier from untrusted text.</summary>
    public static bool TryFrom(string? value, [MaybeNullWhen(false)] out ProfileId id)
    {
        id = null;
        if (value is null || value.Length == 0 || value.Length > MaxLength)
        {
            return false;
        }

        var normalized = value.ToLowerInvariant();
        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];
            var isEdge = i == 0 || i == normalized.Length - 1;
            if (Identifiers.IsLowerAsciiLetterOrDigit(c))
            {
                continue;
            }

            if (!isEdge && (c == '-' || c == '_' || c == '.'))
            {
                continue;
            }

            return false;
        }

        id = new ProfileId(normalized);
        return true;
    }

    /// <inheritdoc />
    public bool Equals(ProfileId? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ProfileId);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Value equality.</summary>
    public static bool operator ==(ProfileId? left, ProfileId? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(ProfileId? left, ProfileId? right) => !(left == right);
}
