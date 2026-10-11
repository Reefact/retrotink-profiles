using System.Diagnostics.CodeAnalysis;

namespace RetroTink.Profiles.Values;

/// <summary>
/// Contract implemented by every Smart Enum so that generic code (serialisers, tooling) can enumerate its members
/// without reflection.
/// </summary>
/// <typeparam name="TSelf">The Smart Enum type itself.</typeparam>
public interface ISmartEnum<TSelf>
    where TSelf : SmartEnum<TSelf>, ISmartEnum<TSelf>
{
    /// <summary>All predefined members, in declaration order.</summary>
    static abstract IReadOnlyList<TSelf> All { get; }
}

/// <summary>
/// Base class of Smart Enums: closed sets of named, immutable instances exposed as static members of their own class,
/// with a stable <see cref="Key"/> and value equality.
/// </summary>
/// <remarks>
/// A Smart Enum knows neither its RT4 byte nor any file-format name. The <see cref="Key"/> is a stable identity that
/// persistence layers may choose to reuse. Keys are lowercase ASCII letters and digits, optionally separated by single hyphens.
/// </remarks>
/// <typeparam name="TSelf">The concrete Smart Enum type.</typeparam>
public abstract class SmartEnum<TSelf> : IEquatable<TSelf>
    where TSelf : SmartEnum<TSelf>, ISmartEnum<TSelf>
{
    /// <summary>Initialises a member.</summary>
    /// <exception cref="ArgumentException">The key is not in canonical form.</exception>
    protected SmartEnum(string key, string displayName)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (!IsValidKey(key))
        {
            throw new ArgumentException($"'{key}' is not a valid Smart Enum key: expected lowercase ASCII letters and digits, optionally separated by single hyphens.", nameof(key));
        }

        Key = key;
        DisplayName = displayName.Trim();
    }

    /// <summary>Stable identity of the member.</summary>
    public string Key { get; }

    /// <summary>Default English label.</summary>
    public string DisplayName { get; }

    /// <summary>Finds the member whose <see cref="Key"/> equals <paramref name="key"/> (ordinal comparison).</summary>
    /// <exception cref="ArgumentException">No member has this key.</exception>
    public static TSelf FromKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return TryFromKey(key, out var value)
            ? value
            : throw new ArgumentException($"'{key}' is not a known {typeof(TSelf).Name} key.", nameof(key));
    }

    /// <summary>Attempts to find the member whose <see cref="Key"/> equals <paramref name="key"/> (ordinal comparison).</summary>
    public static bool TryFromKey(string? key, [MaybeNullWhen(false)] out TSelf value)
    {
        if (key is not null)
        {
            foreach (var candidate in TSelf.All)
            {
                if (string.Equals(candidate.Key, key, StringComparison.Ordinal))
                {
                    value = candidate;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    /// <inheritdoc />
    public bool Equals(TSelf? other) => other is not null && string.Equals(Key, other.Key, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TSelf other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Key);

    /// <inheritdoc />
    public override string ToString() => Key;

    /// <summary>Value equality.</summary>
    public static bool operator ==(SmartEnum<TSelf>? left, SmartEnum<TSelf>? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(SmartEnum<TSelf>? left, SmartEnum<TSelf>? right) => !(left == right);

    private static bool IsValidKey(string key)
    {
        if (key.Length == 0)
        {
            return false;
        }

        var previousWasHyphen = true; // forbids a leading hyphen
        foreach (var c in key)
        {
            if (c == '-')
            {
                if (previousWasHyphen)
                {
                    return false;
                }

                previousWasHyphen = true;
            }
            else if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                previousWasHyphen = false;
            }
            else
            {
                return false;
            }
        }

        return !previousWasHyphen; // forbids a trailing hyphen
    }
}
