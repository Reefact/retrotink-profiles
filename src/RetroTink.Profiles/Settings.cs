using RetroTink.Profiles.Values;

namespace RetroTink.Profiles;

/// <summary>
/// Standard setting definitions shipped with the library, and the catalog grouping them.
/// </summary>
/// <remarks>
/// This is a controlled, deliberately small initial scope. Identifiers are stable; the list grows increment by
/// increment as hardware behaviour is confirmed. None of these definitions implies that a device encoding is known:
/// exportability is decided by format adapters.
/// </remarks>
public static class Settings
{
    /// <summary>Physical input the device acquires from.</summary>
    public static SettingDefinition<VideoInput> Input { get; } =
        new(SettingId.From("input.source"), "Input source");

    /// <summary>Colour standard expected by the analogue decoder (or automatic detection).</summary>
    public static SettingDefinition<VideoStandard> VideoStandard { get; } =
        new(SettingId.From("decode.videoStandard"), "Video standard");

    /// <summary>HDMI output resolution and refresh rate.</summary>
    public static SettingDefinition<OutputResolution> OutputResolution { get; } =
        new(SettingId.From("output.resolution"), "Output resolution");

    /// <summary>HDR signalling on the HDMI output.</summary>
    public static SettingDefinition<HdrMode> Hdr { get; } =
        new(SettingId.From("output.hdr"), "HDR mode");

    /// <summary>Colorimetry signalled on the HDMI output.</summary>
    public static SettingDefinition<Colorimetry> Colorimetry { get; } =
        new(SettingId.From("output.colorimetry"), "Colorimetry");

    /// <summary>RGB quantisation range of the HDMI output.</summary>
    public static SettingDefinition<RgbRange> RgbRange { get; } =
        new(SettingId.From("output.rgbRange"), "RGB range");

    /// <summary>Deinterlacing algorithm; may differ per incoming video mode.</summary>
    public static SettingDefinition<DeinterlacingMode> Deinterlacing { get; } =
        new(SettingId.From("processing.deinterlacing"), "Deinterlacing", SettingScopePolicy.GlobalOrVideoMode);

    /// <summary>Inverse telecine cadence detection; may differ per incoming video mode.</summary>
    public static SettingDefinition<TelecineMode> Telecine { get; } =
        new(SettingId.From("processing.telecine"), "Inverse telecine", SettingScopePolicy.GlobalOrVideoMode);

    /// <summary>How the picture is scaled into the output frame; may differ per incoming video mode.</summary>
    public static SettingDefinition<ScalingMode> ScalingMode { get; } =
        new(SettingId.From("scaling.mode"), "Scaling mode", SettingScopePolicy.GlobalOrVideoMode);

    /// <summary>Display aspect ratio of the source frame; may differ per incoming video mode.</summary>
    public static SettingDefinition<AspectRatio> AspectRatio { get; } =
        new(SettingId.From("geometry.aspectRatio"), "Source aspect ratio", SettingScopePolicy.GlobalOrVideoMode);

    /// <summary>
    /// High-level framing intention (full frame or letterboxed content). An adapter must translate it into concrete
    /// geometry settings and must fail explicitly when no safe translation is known.
    /// </summary>
    public static SettingDefinition<PictureFormat> PictureFormat { get; } =
        new(SettingId.From("geometry.pictureFormat"), "Picture format", SettingScopePolicy.GlobalOrVideoMode, SettingKind.Intent);

    /// <summary>All standard definitions, in a stable order.</summary>
    public static IReadOnlyList<SettingDefinition> All { get; } =
    [
        Input,
        VideoStandard,
        OutputResolution,
        Hdr,
        Colorimetry,
        RgbRange,
        Deinterlacing,
        Telecine,
        ScalingMode,
        AspectRatio,
        PictureFormat,
    ];

    /// <summary>Catalog of all standard definitions. Extend it with <see cref="SettingCatalog.With(SettingDefinition)"/>.</summary>
    public static SettingCatalog Catalog { get; } = SettingCatalog.Create(All);
}
