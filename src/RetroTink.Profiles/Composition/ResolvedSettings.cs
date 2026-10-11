using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using RetroTink.Profiles.Values;

namespace RetroTink.Profiles.Composition;

/// <summary>
/// Effective, read-only view of the settings of a <see cref="ResolvedProfile"/>: local assignments and inherited ones,
/// with provenance.
/// </summary>
/// <remarks>
/// <para>Lookup rule ("profile first, scope second"): layers are visited in priority order (the profile itself, then its
/// sources from the last declared to the first, each with its own ancestors). Within a layer, an assignment for the
/// requested video mode wins over the global one. The first layer holding either wins overall. Hence a global
/// assignment of a child beats a video-mode assignment of a parent.</para>
/// <para>A lookup without context only considers global assignments; it never guesses a video mode.</para>
/// <para>Absence is reported as absence (<c>TryGet</c> returns <see langword="false"/>); no value is ever invented.</para>
/// </remarks>
public sealed class ResolvedSettings
{
    private readonly ProfileId _profile;
    private readonly ImmutableArray<ProfileResolver.Layer> _layers;

    internal ResolvedSettings(ProfileId profile, ImmutableArray<ProfileResolver.Layer> layers)
    {
        _profile = profile;
        _layers = layers;
        Definitions = CollectDefinitions(layers);
    }

    /// <summary>Every setting assigned in at least one layer and scope, ordered by identifier.</summary>
    public IReadOnlyList<SettingDefinition> Definitions { get; }

    /// <summary>Gets the effective global value of <paramref name="definition"/>.</summary>
    /// <exception cref="KeyNotFoundException">The setting is not assigned globally in any layer.</exception>
    public TValue Get<TValue>(SettingDefinition<TValue> definition)
        where TValue : notnull, IEquatable<TValue>
        => TryGet(definition, out var value) ? value : throw new KeyNotFoundException($"Setting '{definition?.Id}' is not assigned (global lookup).");

    /// <summary>Gets the effective value of <paramref name="definition"/> when the device receives <paramref name="context"/>.</summary>
    /// <exception cref="KeyNotFoundException">The setting is assigned neither for the context nor globally in any layer.</exception>
    public TValue Get<TValue>(SettingDefinition<TValue> definition, VideoMode context)
        where TValue : notnull, IEquatable<TValue>
        => TryGet(definition, context, out var value) ? value : throw new KeyNotFoundException($"Setting '{definition?.Id}' is not assigned for '{context?.Key}' nor globally.");

    /// <summary>Attempts to get the effective global value of <paramref name="definition"/>.</summary>
    public bool TryGet<TValue>(SettingDefinition<TValue> definition, [MaybeNullWhen(false)] out TValue value)
        where TValue : notnull, IEquatable<TValue>
        => TryGetCore(definition, null, out value);

    /// <summary>Attempts to get the effective value of <paramref name="definition"/> when the device receives <paramref name="context"/>.</summary>
    public bool TryGet<TValue>(SettingDefinition<TValue> definition, VideoMode context, [MaybeNullWhen(false)] out TValue value)
        where TValue : notnull, IEquatable<TValue>
    {
        ArgumentNullException.ThrowIfNull(context);
        return TryGetCore(definition, context, out value);
    }

    /// <summary>Attempts to explain the effective global value of <paramref name="definition"/>.</summary>
    public bool TryGetProvenance(SettingDefinition definition, [MaybeNullWhen(false)] out SettingProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return TryGetProvenance(definition.Id, null, out provenance);
    }

    /// <summary>Attempts to explain the effective value of <paramref name="definition"/> for <paramref name="context"/>.</summary>
    public bool TryGetProvenance(SettingDefinition definition, VideoMode context, [MaybeNullWhen(false)] out SettingProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        return TryGetProvenance(definition.Id, context, out provenance);
    }

    /// <summary>Attempts to explain the effective value of setting <paramref name="id"/>, globally or for <paramref name="context"/>.</summary>
    public bool TryGetProvenance(SettingId id, VideoMode? context, [MaybeNullWhen(false)] out SettingProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(id);
        var candidates = Candidates(id, context);
        if (candidates.Length == 0)
        {
            provenance = null;
            return false;
        }

        provenance = new SettingProvenance(candidates[0].Assignment.Definition, context, _profile, candidates[0], candidates.RemoveAt(0));
        return true;
    }

    /// <summary>Lists the effective global value of every setting assigned globally in at least one layer, ordered by identifier.</summary>
    public IReadOnlyList<SettingProvenance> ListEffective() => ListEffectiveCore(null);

    /// <summary>Lists the effective value of every assigned setting when the device receives <paramref name="context"/>, ordered by identifier.</summary>
    public IReadOnlyList<SettingProvenance> ListEffective(VideoMode context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ListEffectiveCore(context);
    }

    /// <summary>
    /// Flattens the effective global configuration into standalone, global local assignments (for example to create
    /// an autonomous definition from a resolved one).
    /// </summary>
    public ProfileSettings Flatten() => FlattenCore(null);

    /// <summary>
    /// Flattens the effective configuration for <paramref name="context"/> into standalone, global local assignments.
    /// </summary>
    public ProfileSettings Flatten(VideoMode context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return FlattenCore(context);
    }

    private bool TryGetCore<TValue>(SettingDefinition<TValue> definition, VideoMode? context, [MaybeNullWhen(false)] out TValue value)
        where TValue : notnull, IEquatable<TValue>
    {
        ArgumentNullException.ThrowIfNull(definition);
        var candidates = Candidates(definition.Id, context);
        if (candidates.Length == 0)
        {
            value = default;
            return false;
        }

        var assignment = candidates[0].Assignment;
        if (!assignment.Definition.Equals(definition))
        {
            throw new InvalidOperationException($"Setting '{definition.Id}' is assigned with a different definition (value type {assignment.Definition.ValueType.Name}) than the one requested ({definition.ValueType.Name}).");
        }

        value = ((SettingAssignment<TValue>)assignment).Value;
        return true;
    }

    private ImmutableArray<SettingProvenance> ListEffectiveCore(VideoMode? context)
    {
        var builder = ImmutableArray.CreateBuilder<SettingProvenance>();
        foreach (var definition in Definitions)
        {
            if (TryGetProvenance(definition.Id, context, out var provenance))
            {
                builder.Add(provenance);
            }
        }

        return builder.ToImmutable();
    }

    private ProfileSettings FlattenCore(VideoMode? context)
    {
        var settings = ProfileSettings.Empty;
        foreach (var provenance in ListEffectiveCore(context))
        {
            settings = settings.With(provenance.Effective.Assignment.Accept(Rescoper.Instance));
        }

        return settings;
    }

    /// <summary>Candidates in priority order: for each layer, the context-scoped assignment then the global one.</summary>
    private ImmutableArray<SettingCandidate> Candidates(SettingId id, VideoMode? context)
    {
        var scoped = context is null ? null : SettingScope.For(context);
        var builder = ImmutableArray.CreateBuilder<SettingCandidate>();
        foreach (var layer in _layers)
        {
            if (scoped is not null && layer.Definition.Settings.TryGet(id, scoped, out var scopedAssignment))
            {
                builder.Add(new SettingCandidate(layer.Definition.Id, layer.Via, scopedAssignment));
            }

            if (layer.Definition.Settings.TryGet(id, SettingScope.Global, out var globalAssignment))
            {
                builder.Add(new SettingCandidate(layer.Definition.Id, layer.Via, globalAssignment));
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<SettingDefinition> CollectDefinitions(ImmutableArray<ProfileResolver.Layer> layers)
    {
        var seen = new Dictionary<SettingId, SettingDefinition>();
        foreach (var layer in layers)
        {
            foreach (var assignment in layer.Definition.Settings)
            {
                seen.TryAdd(assignment.SettingId, assignment.Definition);
            }
        }

        return [.. seen.Values.OrderBy(d => d.Id.Value, StringComparer.Ordinal)];
    }

    /// <summary>Re-creates an assignment in the global scope, preserving its static type.</summary>
    private sealed class Rescoper : ISettingAssignmentVisitor<SettingAssignment>
    {
        public static readonly Rescoper Instance = new();

        public SettingAssignment Visit<TValue>(SettingAssignment<TValue> assignment)
            where TValue : notnull, IEquatable<TValue>
            => assignment.Scope.IsGlobal ? assignment : new SettingAssignment<TValue>(assignment.Definition, assignment.Value, SettingScope.Global);
    }
}
