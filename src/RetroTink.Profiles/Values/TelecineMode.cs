namespace RetroTink.Profiles.Values;

/// <summary>Inverse telecine (film cadence) handling.</summary>
public sealed class TelecineMode : SmartEnum<TelecineMode>, ISmartEnum<TelecineMode>
{
    /// <summary>No cadence detection.</summary>
    public static readonly TelecineMode Off = new("off", "Off");

    /// <summary>2:2 cadence (film at 25 fps in 50 Hz video, typical of PAL film transfers).</summary>
    public static readonly TelecineMode Cadence22 = new("cadence-22", "2:2 cadence");

    /// <summary>3:2 cadence (film at 24 fps in 60 Hz video, typical of NTSC film transfers).</summary>
    public static readonly TelecineMode Cadence32 = new("cadence-32", "3:2 cadence");

    private TelecineMode(string key, string displayName)
        : base(key, displayName)
    {
    }

    /// <inheritdoc />
    public static IReadOnlyList<TelecineMode> All { get; } = [Off, Cadence22, Cadence32];
}
