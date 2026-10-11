using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RetroTink.Profiles.Json;

/// <summary>Outcome of reading a JSON document: either the definitions it declares, or errors.</summary>
/// <remarks>
/// Reading is all-or-nothing in schema version 1: any error fails the whole document, and as many errors as possible
/// are reported at once. Parsing does not check that referenced sources exist: that is semantic validation, performed by
/// <see cref="Composition.ProfileResolver"/> on the complete set the application assembles.
/// </remarks>
public sealed class DeserializationResult
{
    private readonly ImmutableArray<ProfileDefinition>? _profiles;

    internal DeserializationResult(ImmutableArray<ProfileDefinition>? profiles, ImmutableArray<JsonDiagnostic> diagnostics)
    {
        _profiles = profiles;
        Diagnostics = diagnostics;
    }

    /// <summary><see langword="true"/> when the document was read without error.</summary>
    public bool IsSuccess => _profiles is not null;

    /// <summary>The definitions, in document order.</summary>
    /// <exception cref="InvalidOperationException">Reading failed; inspect <see cref="Diagnostics"/>.</exception>
    public IReadOnlyList<ProfileDefinition> Profiles => _profiles ?? throw new InvalidOperationException($"The document could not be read: {string.Join(" ", Diagnostics.Select(d => d.ToString()))}");

    /// <summary>All diagnostics, in document order.</summary>
    public IReadOnlyList<JsonDiagnostic> Diagnostics { get; }

    /// <summary>Attempts to get the definitions.</summary>
    public bool TryGetProfiles([MaybeNullWhen(false)] out IReadOnlyList<ProfileDefinition> profiles)
    {
        profiles = _profiles;
        return profiles is not null;
    }

    /// <inheritdoc />
    public override string ToString() => IsSuccess ? $"{_profiles!.Value.Length} profile(s) read" : $"Failed with {Diagnostics.Count} diagnostic(s)";
}
