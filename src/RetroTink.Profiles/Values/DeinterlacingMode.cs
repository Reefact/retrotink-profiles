namespace RetroTink.Profiles.Values;

/// <summary>Deinterlacing algorithm applied to interlaced sources.</summary>
public sealed class DeinterlacingMode : SmartEnum<DeinterlacingMode>, ISmartEnum<DeinterlacingMode>
{
    /// <summary>Motion adaptive deinterlacing.</summary>
    public static readonly DeinterlacingMode MotionAdaptive = new("motion-adaptive", "Motion adaptive");

    /// <summary>Bob (line doubling of each field).</summary>
    public static readonly DeinterlacingMode Bob = new("bob", "Bob");

    /// <summary>Weave (fields merged into frames).</summary>
    public static readonly DeinterlacingMode Weave = new("weave", "Weave");

    private DeinterlacingMode(string key, string displayName)
        : base(key, displayName)
    {
    }

    /// <inheritdoc />
    public static IReadOnlyList<DeinterlacingMode> All { get; } = [MotionAdaptive, Bob, Weave];
}
