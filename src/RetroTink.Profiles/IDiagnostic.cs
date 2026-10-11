namespace RetroTink.Profiles;

/// <summary>
/// Common shape of every diagnostic emitted by the library. Codes are stable identifiers that
/// callers (and a future Studio) can rely on; messages are human readable and may change.
/// </summary>
public interface IDiagnostic
{
    /// <summary>Stable, machine readable code (for example <c>resolution.cyclicSources</c>).</summary>
    string Code { get; }

    /// <summary>Severity of the diagnostic.</summary>
    DiagnosticSeverity Severity { get; }

    /// <summary>Human readable description, in English.</summary>
    string Message { get; }
}
