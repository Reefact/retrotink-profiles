namespace RetroTink.Profiles.Values;

/// <summary>Physical input of the device.</summary>
public sealed class VideoInput : SmartEnum<VideoInput>, ISmartEnum<VideoInput>
{
    /// <summary>Composite video on the front panel.</summary>
    public static readonly VideoInput FrontComposite = new("front-composite", "Front composite");

    /// <summary>S-Video on the front panel.</summary>
    public static readonly VideoInput FrontSVideo = new("front-svideo", "Front S-Video");

    /// <summary>SCART (RGB / composite / S-Video over SCART).</summary>
    public static readonly VideoInput Scart = new("scart", "SCART");

    /// <summary>Component video (YPbPr).</summary>
    public static readonly VideoInput Component = new("component", "Component");

    /// <summary>VGA / RGBHV.</summary>
    public static readonly VideoInput Vga = new("vga", "VGA");

    /// <summary>HDMI.</summary>
    public static readonly VideoInput Hdmi = new("hdmi", "HDMI");

    private VideoInput(string key, string displayName)
        : base(key, displayName)
    {
    }

    /// <inheritdoc />
    public static IReadOnlyList<VideoInput> All { get; } = [FrontComposite, FrontSVideo, Scart, Component, Vga, Hdmi];
}
