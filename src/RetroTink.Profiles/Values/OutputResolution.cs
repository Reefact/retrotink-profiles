namespace RetroTink.Profiles.Values;

/// <summary>HDMI output format: frame size and nominal refresh rate.</summary>
/// <remarks>Which members a given device and firmware can actually output is decided by format adapters.</remarks>
public sealed class OutputResolution : SmartEnum<OutputResolution>, ISmartEnum<OutputResolution>
{
    /// <summary>720×480 progressive at 60 Hz.</summary>
    public static readonly OutputResolution Edtv480p60 = new("480p60", "480p 60 Hz", 720, 480, 60);

    /// <summary>720×576 progressive at 50 Hz.</summary>
    public static readonly OutputResolution Edtv576p50 = new("576p50", "576p 50 Hz", 720, 576, 50);

    /// <summary>1280×720 progressive at 50 Hz.</summary>
    public static readonly OutputResolution Hd720p50 = new("720p50", "720p 50 Hz", 1280, 720, 50);

    /// <summary>1280×720 progressive at 60 Hz.</summary>
    public static readonly OutputResolution Hd720p60 = new("720p60", "720p 60 Hz", 1280, 720, 60);

    /// <summary>1920×1080 progressive at 50 Hz.</summary>
    public static readonly OutputResolution FullHd50 = new("1080p50", "1080p 50 Hz", 1920, 1080, 50);

    /// <summary>1920×1080 progressive at 60 Hz.</summary>
    public static readonly OutputResolution FullHd60 = new("1080p60", "1080p 60 Hz", 1920, 1080, 60);

    /// <summary>3840×2160 progressive at 50 Hz.</summary>
    public static readonly OutputResolution Uhd50 = new("2160p50", "2160p 50 Hz", 3840, 2160, 50);

    /// <summary>3840×2160 progressive at 60 Hz.</summary>
    public static readonly OutputResolution Uhd60 = new("2160p60", "2160p 60 Hz", 3840, 2160, 60);

    private OutputResolution(string key, string displayName, int width, int height, int refreshRateHz)
        : base(key, displayName)
    {
        Width = width;
        Height = height;
        RefreshRateHz = refreshRateHz;
    }

    /// <inheritdoc />
    public static IReadOnlyList<OutputResolution> All { get; } = [Edtv480p60, Edtv576p50, Hd720p50, Hd720p60, FullHd50, FullHd60, Uhd50, Uhd60];

    /// <summary>Frame width in pixels.</summary>
    public int Width { get; }

    /// <summary>Frame height in pixels.</summary>
    public int Height { get; }

    /// <summary>Nominal refresh rate in hertz (60 stands for 59.94 Hz where applicable).</summary>
    public int RefreshRateHz { get; }
}
