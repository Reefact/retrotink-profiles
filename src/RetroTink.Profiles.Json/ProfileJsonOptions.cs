namespace RetroTink.Profiles.Json;

/// <summary>Options of <see cref="ProfileJsonSerializer"/>.</summary>
public sealed class ProfileJsonOptions
{
    /// <summary>Default options: default converters, indented output, 16 MiB input limit.</summary>
    public static ProfileJsonOptions Default { get; } = new();

    /// <summary>Converters used for setting values. Defaults to <see cref="JsonSettingValueConverters.Default"/>.</summary>
    public JsonSettingValueConverters Converters { get; init; } = JsonSettingValueConverters.Default;

    /// <summary>Whether written JSON is indented (recommended for Git-friendly files). Defaults to <see langword="true"/>.</summary>
    public bool WriteIndented { get; init; } = true;

    /// <summary>
    /// Maximum accepted input size, in bytes (UTF-16 characters for string input). Larger documents are rejected
    /// before parsing with <see cref="JsonDiagnosticCodes.DocumentTooLarge"/>. Defaults to 16 MiB.
    /// </summary>
    public int MaxDocumentSizeInBytes { get; init; } = 16 * 1024 * 1024;

    /// <summary>Maximum nesting depth accepted when parsing. Defaults to 16 (the schema needs 6).</summary>
    public int MaxDepth { get; init; } = 16;
}
