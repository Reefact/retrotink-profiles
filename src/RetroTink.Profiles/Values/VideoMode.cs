namespace RetroTink.Profiles.Values;

/// <summary>Incoming video mode, used to scope a setting to a given kind of source signal.</summary>
/// <remarks>
/// The initial list covers standard-definition modes relevant to analogue sources. Which settings a device really
/// stores per mode is a firmware question for format adapters; the model only needs distinct, non-overlapping modes.
/// </remarks>
public sealed class VideoMode : SmartEnum<VideoMode>, ISmartEnum<VideoMode>
{
    /// <summary>480 interlaced lines at 59.94 Hz (NTSC).</summary>
    public static readonly VideoMode Ntsc480i = new("ntsc480i", "480i (NTSC)", 480, true, 60);

    /// <summary>576 interlaced lines at 50 Hz (PAL).</summary>
    public static readonly VideoMode Pal576i = new("pal576i", "576i (PAL)", 576, true, 50);

    /// <summary>480 progressive lines at 59.94 Hz.</summary>
    public static readonly VideoMode Edtv480p = new("edtv480p", "480p", 480, false, 60);

    /// <summary>576 progressive lines at 50 Hz.</summary>
    public static readonly VideoMode Edtv576p = new("edtv576p", "576p", 576, false, 50);

    private VideoMode(string key, string displayName, int activeLines, bool isInterlaced, int refreshRateHz)
        : base(key, displayName)
    {
        ActiveLines = activeLines;
        IsInterlaced = isInterlaced;
        RefreshRateHz = refreshRateHz;
    }

    /// <inheritdoc />
    public static IReadOnlyList<VideoMode> All { get; } = [Ntsc480i, Pal576i, Edtv480p, Edtv576p];

    /// <summary>Number of active lines.</summary>
    public int ActiveLines { get; }

    /// <summary><see langword="true"/> for interlaced modes.</summary>
    public bool IsInterlaced { get; }

    /// <summary>Nominal field or frame rate in hertz (60 stands for 59.94 Hz).</summary>
    public int RefreshRateHz { get; }
}
