namespace RetroTink.Profiles.Values;

/// <summary>How the picture is fitted into the output frame.</summary>
public sealed class ScalingMode : SmartEnum<ScalingMode>, ISmartEnum<ScalingMode>
{
    /// <summary>Keep the source aspect ratio.</summary>
    public static readonly ScalingMode Proportional = new("proportional", "Proportional");

    /// <summary>Fill the output frame.</summary>
    public static readonly ScalingMode AutoFill = new("auto-fill", "Auto fill");

    /// <summary>Integer multiples of the source size.</summary>
    public static readonly ScalingMode IntegerMultiple = new("integer", "Integer multiple");

    private ScalingMode(string key, string displayName)
        : base(key, displayName)
    {
    }

    /// <inheritdoc />
    public static IReadOnlyList<ScalingMode> All { get; } = [Proportional, AutoFill, IntegerMultiple];
}
