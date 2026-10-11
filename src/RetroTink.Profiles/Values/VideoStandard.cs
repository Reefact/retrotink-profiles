namespace RetroTink.Profiles.Values;

/// <summary>Analogue colour standard expected by the decoder.</summary>
/// <remarks>
/// <see cref="Auto"/> is an explicit value: it differs from the absence of an assignment, which means "inherit".
/// Whether a device persists this choice at all is a question for format adapters, not for the model.
/// </remarks>
public sealed class VideoStandard : SmartEnum<VideoStandard>, ISmartEnum<VideoStandard>
{
    /// <summary>Automatic detection by the device.</summary>
    public static readonly VideoStandard Auto = new("auto", "Auto");

    /// <summary>NTSC (525 lines, 59.94 Hz).</summary>
    public static readonly VideoStandard Ntsc = new("ntsc", "NTSC");

    /// <summary>PAL (625 lines, 50 Hz).</summary>
    public static readonly VideoStandard Pal = new("pal", "PAL");

    private VideoStandard(string key, string displayName)
        : base(key, displayName)
    {
    }

    /// <inheritdoc />
    public static IReadOnlyList<VideoStandard> All { get; } = [Auto, Ntsc, Pal];
}
