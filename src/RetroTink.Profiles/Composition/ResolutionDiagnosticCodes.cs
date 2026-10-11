namespace RetroTink.Profiles.Composition;

/// <summary>Stable codes of the diagnostics emitted by <see cref="ProfileResolver"/>.</summary>
public static class ResolutionDiagnosticCodes
{
    /// <summary>The requested profile is not part of the set. Error.</summary>
    public const string UnknownProfile = "resolution.unknownProfile";

    /// <summary>A profile references a source that is not part of the set. Error.</summary>
    public const string MissingSource = "resolution.missingSource";

    /// <summary>Following sources leads back to a profile already on the path. Error.</summary>
    public const string CyclicSources = "resolution.cyclicSources";

    /// <summary>Several definitions in the set share the same identifier. Error.</summary>
    public const string DuplicateProfileId = "resolution.duplicateProfileId";

    /// <summary>Two profiles use the same setting identifier with incompatible definitions. Error.</summary>
    public const string IncompatibleSettingDefinitions = "resolution.incompatibleSettingDefinitions";

    /// <summary>Two sources of a profile assign different values to the same setting and scope. Warning.</summary>
    public const string SourceConflict = "resolution.sourceConflict";
}
