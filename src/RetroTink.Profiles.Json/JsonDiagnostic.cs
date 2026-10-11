namespace RetroTink.Profiles.Json;

/// <summary>A diagnostic produced while reading a JSON document. See <see cref="JsonDiagnosticCodes"/>.</summary>
public sealed class JsonDiagnostic : IDiagnostic
{
    internal JsonDiagnostic(string code, DiagnosticSeverity severity, string message, string path, string? profileId, string? settingId)
    {
        Code = code;
        Severity = severity;
        Message = message;
        Path = path;
        ProfileId = profileId;
        SettingId = settingId;
    }

    /// <inheritdoc />
    public string Code { get; }

    /// <inheritdoc />
    public DiagnosticSeverity Severity { get; }

    /// <inheritdoc />
    public string Message { get; }

    /// <summary>JSON path of the offending element, for example <c>$.profiles[1].settings[0].value</c>.</summary>
    public string Path { get; }

    /// <summary>Raw identifier of the profile being read, when known (it may itself be invalid).</summary>
    public string? ProfileId { get; }

    /// <summary>Raw identifier of the setting being read, when known (it may itself be invalid).</summary>
    public string? SettingId { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Severity} {Code} at {Path}: {Message}";
}
