namespace RetroTink.Profiles;

/// <summary>Nature of a setting with respect to the hardware.</summary>
public enum SettingKind
{
    /// <summary>A concrete device setting that a format adapter may map directly, once its encoding is confirmed.</summary>
    Hardware = 0,

    /// <summary>
    /// A high-level intention (for example a letterboxed picture format) that an adapter must translate into one or
    /// several device settings, and must refuse explicitly when no safe translation is known.
    /// </summary>
    Intent = 1,
}
