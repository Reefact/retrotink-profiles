using System.Collections.Immutable;

namespace RetroTink.Profiles.Composition;

/// <summary>
/// Pure, deterministic composition of <see cref="ProfileDefinition"/>s: follows sources, detects missing references and
/// cycles, computes effective values with provenance and reports conflicts. No I/O, no repository.
/// </summary>
/// <remarks>
/// <para><b>Algorithm.</b> A profile's configuration is the composition of the effective configurations of its sources,
/// applied from the first declared to the last (the last wins), with the profile's local assignments on top. This is
/// implemented as a depth-first walk producing an ordered list of layers: the profile, then for each source from the
/// last to the first, the source followed by its own layers. A definition reached several times (diamond inheritance)
/// keeps its first, strongest position; repeating an ancestor is not a cycle. Values are looked up layer by layer, see
/// <see cref="ResolvedSettings"/> for the scope rule.</para>
/// <para><b>Diagnostics.</b> Unknown profile, missing source, cycle, duplicate identifier and incompatible setting
/// definitions are errors: no profile is produced. Two sources of a profile (at any level) that assign different
/// values to the same setting and scope produce a non-blocking <see cref="ResolutionDiagnosticCodes.SourceConflict"/>
/// warning, even when the profile overrides the value locally. Equal values coming from different sources, or from a
/// shared ancestor, are not reported.</para>
/// </remarks>
public sealed class ProfileResolver
{
    private readonly ImmutableDictionary<ProfileId, ProfileDefinition> _byId;
    private readonly ImmutableArray<ResolutionDiagnostic> _setDiagnostics;

    /// <summary>Creates a resolver over an in-memory set of definitions.</summary>
    /// <remarks>Duplicated identifiers are not rejected here; every resolution then fails with
    /// <see cref="ResolutionDiagnosticCodes.DuplicateProfileId"/>, so that the problem is reported as a diagnostic.</remarks>
    public ProfileResolver(IEnumerable<ProfileDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var all = ImmutableArray.CreateBuilder<ProfileDefinition>();
        var byId = ImmutableDictionary.CreateBuilder<ProfileId, ProfileDefinition>();
        var counts = new Dictionary<ProfileId, int>();
        foreach (var definition in definitions)
        {
            ArgumentNullException.ThrowIfNull(definition, nameof(definitions));
            all.Add(definition);
            counts[definition.Id] = counts.GetValueOrDefault(definition.Id) + 1;
            byId.TryAdd(definition.Id, definition);
        }

        Definitions = all.ToImmutable();
        _byId = byId.ToImmutable();
        _setDiagnostics = [.. all.Select(d => d.Id).Distinct().Where(id => counts[id] > 1).Select(id => ResolutionDiagnostic.DuplicateProfileId(id, counts[id]))];
    }

    /// <summary>The definitions given to the resolver, in their original order.</summary>
    public IReadOnlyList<ProfileDefinition> Definitions { get; }

    /// <summary>Resolves the profile identified by <paramref name="id"/>.</summary>
    public ResolutionResult Resolve(ProfileId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (_setDiagnostics.Length > 0)
        {
            return new ResolutionResult(id, null, _setDiagnostics);
        }

        if (!_byId.TryGetValue(id, out var target))
        {
            return new ResolutionResult(id, null, [ResolutionDiagnostic.UnknownProfile(id)]);
        }

        var walk = new Walk(_byId);
        walk.Visit(target, null);
        if (walk.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return new ResolutionResult(id, null, [.. walk.Diagnostics]);
        }

        var layers = walk.Layers.ToImmutableArray();
        var diagnostics = ImmutableArray.CreateBuilder<ResolutionDiagnostic>();
        diagnostics.AddRange(walk.Diagnostics);
        foreach (var layer in layers)
        {
            if (layer.Definition.Sources.Count >= 2)
            {
                DetectConflicts(layer.Definition, diagnostics);
            }
        }

        return new ResolutionResult(id, new ResolvedProfile(target, layers), diagnostics.ToImmutable());
    }

    /// <summary>Layers of <paramref name="root"/>, for a graph already known to be valid.</summary>
    private ImmutableArray<ProfileDefinition> LayersOf(ProfileDefinition root)
    {
        var layers = ImmutableArray.CreateBuilder<ProfileDefinition>();
        var visited = new HashSet<ProfileId>();
        Collect(root);
        return layers.ToImmutable();

        void Collect(ProfileDefinition definition)
        {
            visited.Add(definition.Id);
            layers.Add(definition);
            for (var i = definition.Sources.Count - 1; i >= 0; i--)
            {
                var source = definition.Sources[i];
                if (!visited.Contains(source) && _byId.TryGetValue(source, out var sourceDefinition))
                {
                    Collect(sourceDefinition);
                }
            }
        }
    }

    private void DetectConflicts(ProfileDefinition profile, ImmutableArray<ResolutionDiagnostic>.Builder diagnostics)
    {
        var perSource = profile.Sources.Select(s => (Source: s, Layers: LayersOf(_byId[s]))).ToArray();

        // Keys in a deterministic order: first appearance across sources, layers and local assignments.
        var keys = new List<(SettingId Id, SettingScope Scope)>();
        var seen = new HashSet<(SettingId, SettingScope)>();
        foreach (var (_, layers) in perSource)
        {
            foreach (var layer in layers)
            {
                foreach (var assignment in layer.Settings)
                {
                    if (seen.Add((assignment.SettingId, assignment.Scope)))
                    {
                        keys.Add((assignment.SettingId, assignment.Scope));
                    }
                }
            }
        }

        foreach (var (id, scope) in keys)
        {
            var candidates = ImmutableArray.CreateBuilder<SettingCandidate>();
            foreach (var (source, layers) in perSource)
            {
                foreach (var layer in layers)
                {
                    if (layer.Settings.TryGet(id, scope, out var assignment))
                    {
                        candidates.Add(new SettingCandidate(layer.Id, source, assignment));
                        break;
                    }
                }
            }

            if (candidates.Count < 2 || candidates.Select(c => c.Assignment).Distinct().Count() < 2)
            {
                continue;
            }

            var winner = candidates[^1];
            var resolvedLocally = profile.Settings.Contains(id, scope);
            diagnostics.Add(ResolutionDiagnostic.SourceConflict(profile.Id, id, scope, candidates.ToImmutable(), winner, resolvedLocally));
        }
    }

    /// <summary>A contributing definition and the direct source of the resolved profile it was reached through.</summary>
    internal readonly record struct Layer(ProfileDefinition Definition, ProfileId? Via);

    private sealed class Walk(ImmutableDictionary<ProfileId, ProfileDefinition> byId)
    {
        private readonly HashSet<ProfileId> _visited = [];
        private readonly List<ProfileId> _path = [];
        private readonly Dictionary<SettingId, (ProfileId Profile, SettingDefinition Definition)> _definitions = [];

        public List<Layer> Layers { get; } = [];

        public List<ResolutionDiagnostic> Diagnostics { get; } = [];

        public void Visit(ProfileDefinition definition, ProfileId? via)
        {
            _visited.Add(definition.Id);
            _path.Add(definition.Id);
            Layers.Add(new Layer(definition, via));
            CheckSettingDefinitions(definition);

            for (var i = definition.Sources.Count - 1; i >= 0; i--)
            {
                var source = definition.Sources[i];
                var indexOnPath = _path.IndexOf(source);
                if (indexOnPath >= 0)
                {
                    Diagnostics.Add(ResolutionDiagnostic.CyclicSources([.. _path.Skip(indexOnPath), source]));
                    continue;
                }

                if (_visited.Contains(source))
                {
                    continue; // diamond: already contributed at a stronger position
                }

                if (!byId.TryGetValue(source, out var sourceDefinition))
                {
                    Diagnostics.Add(ResolutionDiagnostic.MissingSource(definition.Id, source));
                    continue;
                }

                Visit(sourceDefinition, via ?? source);
            }

            _path.RemoveAt(_path.Count - 1);
        }

        private void CheckSettingDefinitions(ProfileDefinition definition)
        {
            foreach (var assignment in definition.Settings)
            {
                if (_definitions.TryGetValue(assignment.SettingId, out var existing))
                {
                    if (!existing.Definition.Equals(assignment.Definition))
                    {
                        Diagnostics.Add(ResolutionDiagnostic.IncompatibleSettingDefinitions(assignment.SettingId, existing.Profile, existing.Definition, definition.Id, assignment.Definition));
                    }
                }
                else
                {
                    _definitions.Add(assignment.SettingId, (definition.Id, assignment.Definition));
                }
            }
        }
    }
}
