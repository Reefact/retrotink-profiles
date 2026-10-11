namespace RetroTink.Profiles.Values;

/// <summary>Colorimetry signalled on the HDMI output.</summary>
public sealed class Colorimetry : SmartEnum<Colorimetry>, ISmartEnum<Colorimetry>
{
    /// <summary>Chosen automatically by the device.</summary>
    public static readonly Colorimetry Auto = new("auto", "Auto");

    /// <summary>ITU-R BT.601 (standard definition).</summary>
    public static readonly Colorimetry Rec601 = new("rec601", "Rec. 601");

    /// <summary>ITU-R BT.709 (high definition).</summary>
    public static readonly Colorimetry Rec709 = new("rec709", "Rec. 709");

    /// <summary>ITU-R BT.2020 (ultra high definition / HDR).</summary>
    public static readonly Colorimetry Rec2020 = new("rec2020", "Rec. 2020");

    private Colorimetry(string key, string displayName)
        : base(key, displayName)
    {
    }

    /// <inheritdoc />
    public static IReadOnlyList<Colorimetry> All { get; } = [Auto, Rec601, Rec709, Rec2020];
}
