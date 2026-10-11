namespace RetroTink.Profiles.Values;

/// <summary>RGB quantisation range of the HDMI output.</summary>
public sealed class RgbRange : SmartEnum<RgbRange>, ISmartEnum<RgbRange>
{
    /// <summary>Chosen automatically by the device.</summary>
    public static readonly RgbRange Auto = new("auto", "Auto");

    /// <summary>Limited range (16–235).</summary>
    public static readonly RgbRange Limited = new("limited", "Limited");

    /// <summary>Full range (0–255).</summary>
    public static readonly RgbRange Full = new("full", "Full");

    private RgbRange(string key, string displayName)
        : base(key, displayName)
    {
    }

    /// <inheritdoc />
    public static IReadOnlyList<RgbRange> All { get; } = [Auto, Limited, Full];
}
