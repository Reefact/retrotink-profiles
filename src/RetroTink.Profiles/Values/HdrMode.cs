namespace RetroTink.Profiles.Values;

/// <summary>HDR signalling on the HDMI output. <see cref="Off"/> is an explicit value, distinct from "not assigned".</summary>
public sealed class HdrMode : SmartEnum<HdrMode>, ISmartEnum<HdrMode>
{
    /// <summary>No HDR metadata (SDR output).</summary>
    public static readonly HdrMode Off = new("off", "Off");

    /// <summary>HDR10 (PQ) signalling.</summary>
    public static readonly HdrMode Hdr10 = new("hdr10", "HDR10");

    /// <summary>Hybrid Log-Gamma signalling.</summary>
    public static readonly HdrMode Hlg = new("hlg", "HLG");

    private HdrMode(string key, string displayName)
        : base(key, displayName)
    {
    }

    /// <inheritdoc />
    public static IReadOnlyList<HdrMode> All { get; } = [Off, Hdr10, Hlg];
}
