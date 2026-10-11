namespace RetroTink.Profiles.Values;

/// <summary>Kind of <see cref="PictureFormat"/>.</summary>
public enum PictureFormatKind
{
    /// <summary>The content fills the source frame.</summary>
    FullFrame = 0,

    /// <summary>The content is letterboxed inside the source frame, with the given content aspect ratio.</summary>
    Letterboxed = 1,
}

/// <summary>
/// High-level framing intention describing how the picture content sits inside the source frame.
/// </summary>
/// <remarks>
/// This is an intention, not a device setting: there is no single device field behind it. Adapters translate it
/// (zoom, crop, aspect) or fail explicitly.
/// </remarks>
public sealed class PictureFormat : IEquatable<PictureFormat>
{
    private PictureFormat(PictureFormatKind kind, AspectRatio? contentAspectRatio)
    {
        Kind = kind;
        ContentAspectRatio = contentAspectRatio;
    }

    /// <summary>The content fills the source frame (for example a 4:3 film in a 4:3 frame).</summary>
    public static PictureFormat FullFrame { get; } = new(PictureFormatKind.FullFrame, null);

    /// <summary>Kind of framing.</summary>
    public PictureFormatKind Kind { get; }

    /// <summary>Aspect ratio of the letterboxed content; <see langword="null"/> for <see cref="FullFrame"/>.</summary>
    public AspectRatio? ContentAspectRatio { get; }

    /// <summary><see langword="true"/> when the content is letterboxed.</summary>
    public bool IsLetterboxed => Kind == PictureFormatKind.Letterboxed;

    /// <summary>The content is letterboxed inside the frame with the given aspect ratio (for example 2.35:1 in a 4:3 frame).</summary>
    public static PictureFormat Letterboxed(AspectRatio contentAspectRatio)
    {
        ArgumentNullException.ThrowIfNull(contentAspectRatio);
        return new PictureFormat(PictureFormatKind.Letterboxed, contentAspectRatio);
    }

    /// <inheritdoc />
    public bool Equals(PictureFormat? other) => other is not null && Kind == other.Kind && Equals(ContentAspectRatio, other.ContentAspectRatio);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as PictureFormat);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, ContentAspectRatio);

    /// <inheritdoc />
    public override string ToString() => Kind == PictureFormatKind.FullFrame ? "full-frame" : $"letterboxed({ContentAspectRatio})";

    /// <summary>Value equality.</summary>
    public static bool operator ==(PictureFormat? left, PictureFormat? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(PictureFormat? left, PictureFormat? right) => !(left == right);
}
