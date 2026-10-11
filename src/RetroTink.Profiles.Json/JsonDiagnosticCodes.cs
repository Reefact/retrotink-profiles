namespace RetroTink.Profiles.Json;

/// <summary>Stable codes of the diagnostics emitted when reading a JSON document.</summary>
public static class JsonDiagnosticCodes
{
    /// <summary>The input is not well-formed JSON (or not valid UTF-8).</summary>
    public const string Malformed = "json.malformed";

    /// <summary>The input exceeds the configured size limit.</summary>
    public const string DocumentTooLarge = "json.documentTooLarge";

    /// <summary>A JSON value has not the expected type or a required property is missing.</summary>
    public const string InvalidDocument = "json.invalidDocument";

    /// <summary>A property is not part of the schema.</summary>
    public const string UnknownProperty = "json.unknownProperty";

    /// <summary>The same property appears twice in one object.</summary>
    public const string DuplicateProperty = "json.duplicateProperty";

    /// <summary>The schema version is missing, not an integer, or not supported by this build.</summary>
    public const string UnsupportedSchemaVersion = "json.unsupportedSchemaVersion";

    /// <summary>A profile identifier is not valid.</summary>
    public const string InvalidProfileId = "json.invalidProfileId";

    /// <summary>A profile name is not valid.</summary>
    public const string InvalidProfileName = "json.invalidProfileName";

    /// <summary>Two profiles of the document share an identifier.</summary>
    public const string DuplicateProfileId = "json.duplicateProfileId";

    /// <summary>A source is listed twice, or a profile lists itself as a source.</summary>
    public const string InvalidSources = "json.invalidSources";

    /// <summary>A setting identifier is not valid.</summary>
    public const string InvalidSettingId = "json.invalidSettingId";

    /// <summary>A setting identifier is not declared in the catalog.</summary>
    public const string UnknownSetting = "json.unknownSetting";

    /// <summary>A scope object is malformed or names an unknown video mode.</summary>
    public const string InvalidScope = "json.invalidScope";

    /// <summary>The scope is not admissible for the setting.</summary>
    public const string ScopeNotAllowed = "json.scopeNotAllowed";

    /// <summary>The same setting and scope are assigned twice in one profile.</summary>
    public const string DuplicateSetting = "json.duplicateSetting";

    /// <summary>A value cannot be converted to the setting's value type.</summary>
    public const string InvalidValue = "json.invalidValue";
}
