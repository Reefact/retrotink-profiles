namespace RetroTink.Profiles;

/// <summary>Severity of a diagnostic produced by resolution, validation or persistence.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational note; never affects the outcome.</summary>
    Info = 0,

    /// <summary>Something worth the attention of the user, but the operation succeeded.</summary>
    Warning = 1,

    /// <summary>The operation failed; no trustworthy result is available.</summary>
    Error = 2,
}
