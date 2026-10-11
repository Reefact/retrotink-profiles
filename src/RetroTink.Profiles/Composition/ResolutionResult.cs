using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RetroTink.Profiles.Composition;

/// <summary>Outcome of <see cref="ProfileResolver.Resolve"/>: either a profile (possibly with warnings) or errors.</summary>
public sealed class ResolutionResult
{
    private readonly ResolvedProfile? _profile;

    internal ResolutionResult(ProfileId requested, ResolvedProfile? profile, ImmutableArray<ResolutionDiagnostic> diagnostics)
    {
        Requested = requested;
        _profile = profile;
        Diagnostics = diagnostics;
    }

    /// <summary>The identifier that was asked for.</summary>
    public ProfileId Requested { get; }

    /// <summary><see langword="true"/> when a profile is available. Warnings do not prevent success.</summary>
    public bool IsSuccess => _profile is not null;

    /// <summary>The resolved profile.</summary>
    /// <exception cref="InvalidOperationException">The resolution failed; inspect <see cref="Diagnostics"/>.</exception>
    public ResolvedProfile Profile => _profile ?? throw new InvalidOperationException($"Resolution of '{Requested}' failed: {string.Join(" ", Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message))}");

    /// <summary>All diagnostics, in a deterministic order.</summary>
    public IReadOnlyList<ResolutionDiagnostic> Diagnostics { get; }

    /// <summary>Error diagnostics only.</summary>
    public IEnumerable<ResolutionDiagnostic> Errors => Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>Warning diagnostics only.</summary>
    public IEnumerable<ResolutionDiagnostic> Warnings => Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>Attempts to get the resolved profile.</summary>
    public bool TryGetProfile([MaybeNullWhen(false)] out ResolvedProfile profile)
    {
        profile = _profile;
        return profile is not null;
    }

    /// <inheritdoc />
    public override string ToString() => IsSuccess ? $"Resolved '{Requested}' with {Diagnostics.Count} diagnostic(s)" : $"Failed to resolve '{Requested}': {Errors.Count()} error(s)";
}
