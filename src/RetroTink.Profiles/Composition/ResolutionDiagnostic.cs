using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace RetroTink.Profiles.Composition;

/// <summary>A diagnostic produced while resolving a profile. See <see cref="ResolutionDiagnosticCodes"/>.</summary>
public sealed class ResolutionDiagnostic : IDiagnostic
{
    private ResolutionDiagnostic(string code, DiagnosticSeverity severity, string message, ProfileId? profile, SettingId? setting, SettingScope? scope, ImmutableArray<ProfileId> relatedProfiles, ImmutableArray<SettingCandidate> candidates)
    {
        Code = code;
        Severity = severity;
        Message = message;
        Profile = profile;
        Setting = setting;
        Scope = scope;
        RelatedProfiles = relatedProfiles;
        Candidates = candidates;
    }

    /// <inheritdoc />
    public string Code { get; }

    /// <inheritdoc />
    public DiagnosticSeverity Severity { get; }

    /// <inheritdoc />
    public string Message { get; }

    /// <summary>The profile the diagnostic is attached to, when relevant.</summary>
    public ProfileId? Profile { get; }

    /// <summary>The setting involved, when relevant.</summary>
    public SettingId? Setting { get; }

    /// <summary>The scope involved, when relevant.</summary>
    public SettingScope? Scope { get; }

    /// <summary>
    /// Other profiles involved: the cycle path for <see cref="ResolutionDiagnosticCodes.CyclicSources"/>, the missing
    /// identifier for <see cref="ResolutionDiagnosticCodes.MissingSource"/>, the disagreeing sources for
    /// <see cref="ResolutionDiagnosticCodes.SourceConflict"/>, and so on.
    /// </summary>
    public IReadOnlyList<ProfileId> RelatedProfiles { get; }

    /// <summary>Competing values for <see cref="ResolutionDiagnosticCodes.SourceConflict"/>; empty otherwise.</summary>
    public IReadOnlyList<SettingCandidate> Candidates { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Severity} {Code}: {Message}";

    internal static ResolutionDiagnostic UnknownProfile(ProfileId id) =>
        new(ResolutionDiagnosticCodes.UnknownProfile, DiagnosticSeverity.Error,
            $"Profile '{id}' is not part of the set to resolve.",
            id, null, null, [], []);

    internal static ResolutionDiagnostic MissingSource(ProfileId profile, ProfileId source) =>
        new(ResolutionDiagnosticCodes.MissingSource, DiagnosticSeverity.Error,
            $"Profile '{profile}' references source '{source}', which is not part of the set.",
            profile, null, null, [source], []);

    internal static ResolutionDiagnostic CyclicSources(ImmutableArray<ProfileId> path) =>
        new(ResolutionDiagnosticCodes.CyclicSources, DiagnosticSeverity.Error,
            $"Sources form a cycle: {string.Join(" -> ", path.Select(p => $"'{p}'"))}.",
            path[0], null, null, path, []);

    internal static ResolutionDiagnostic DuplicateProfileId(ProfileId id, int count) =>
        new(ResolutionDiagnosticCodes.DuplicateProfileId, DiagnosticSeverity.Error,
            string.Create(CultureInfo.InvariantCulture, $"{count} definitions in the set share the id '{id}'."),
            id, null, null, [], []);

    internal static ResolutionDiagnostic IncompatibleSettingDefinitions(SettingId setting, ProfileId first, SettingDefinition firstDefinition, ProfileId second, SettingDefinition secondDefinition) =>
        new(ResolutionDiagnosticCodes.IncompatibleSettingDefinitions, DiagnosticSeverity.Error,
            $"Setting '{setting}' is declared with incompatible definitions: '{first}' uses value type {firstDefinition.ValueType.Name} ({firstDefinition.ScopePolicy}, {firstDefinition.Kind}, \"{firstDefinition.DisplayName}\") while '{second}' uses {secondDefinition.ValueType.Name} ({secondDefinition.ScopePolicy}, {secondDefinition.Kind}, \"{secondDefinition.DisplayName}\").",
            second, setting, null, [first, second], []);

    internal static ResolutionDiagnostic SourceConflict(ProfileId profile, SettingId setting, SettingScope scope, ImmutableArray<SettingCandidate> candidates, SettingCandidate winner, bool resolvedLocally)
    {
        var message = new StringBuilder();
        message.Append(CultureInfo.InvariantCulture, $"Sources of profile '{profile}' disagree on '{setting}' ({scope}): ");
        message.AppendJoin(", ", candidates.Select(Describe));
        message.Append(CultureInfo.InvariantCulture, $". '{winner.Via}' takes precedence");
        if (winner.Via is not null && !winner.Via.Equals(winner.Source))
        {
            message.Append(CultureInfo.InvariantCulture, $" (value declared by '{winner.Source}')");
        }

        message.Append(resolvedLocally ? "; the profile overrides the setting locally anyway." : ".");

        return new ResolutionDiagnostic(ResolutionDiagnosticCodes.SourceConflict, DiagnosticSeverity.Warning,
            message.ToString(), profile, setting, scope, [.. candidates.Select(c => c.Via!)], candidates);

        static string Describe(SettingCandidate candidate) =>
            candidate.Via is null || candidate.Via.Equals(candidate.Source)
                ? $"'{candidate.Source}' -> {candidate.Assignment.FormatValue()}"
                : $"'{candidate.Via}' -> {candidate.Assignment.FormatValue()} (from '{candidate.Source}')";
    }
}
