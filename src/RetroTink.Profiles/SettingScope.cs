using RetroTink.Profiles.Values;

namespace RetroTink.Profiles;

/// <summary>
/// Context in which a setting assignment applies: either everywhere (<see cref="Global"/>) or only when the
/// device receives a given <see cref="Values.VideoMode"/>.
/// </summary>
/// <remarks>
/// Overlapping or combinable scopes are deliberately not supported in this version; precedence between a
/// video-mode scope and the global scope is defined by <see cref="Composition.ProfileResolver"/> ("profile first, scope second").
/// </remarks>
public sealed class SettingScope : IEquatable<SettingScope>
{
    private SettingScope(VideoMode? videoMode) => VideoMode = videoMode;

    /// <summary>The scope that applies regardless of the incoming video mode. Implicit when no scope is given.</summary>
    public static SettingScope Global { get; } = new(null);

    /// <summary>The video mode this scope is restricted to, or <see langword="null"/> for the global scope.</summary>
    public VideoMode? VideoMode { get; }

    /// <summary><see langword="true"/> for the global scope.</summary>
    public bool IsGlobal => VideoMode is null;

    /// <summary>Creates the scope restricted to <paramref name="videoMode"/>.</summary>
    public static SettingScope For(VideoMode videoMode)
    {
        ArgumentNullException.ThrowIfNull(videoMode);
        return new SettingScope(videoMode);
    }

    /// <inheritdoc />
    public bool Equals(SettingScope? other) => other is not null && Equals(VideoMode, other.VideoMode);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SettingScope);

    /// <inheritdoc />
    public override int GetHashCode() => VideoMode is null ? 0 : VideoMode.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => VideoMode is null ? "global" : $"videoMode:{VideoMode.Key}";

    /// <summary>Value equality.</summary>
    public static bool operator ==(SettingScope? left, SettingScope? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(SettingScope? left, SettingScope? right) => !(left == right);
}
