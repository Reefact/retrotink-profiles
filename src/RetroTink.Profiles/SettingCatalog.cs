using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RetroTink.Profiles;

/// <summary>
/// Immutable, validated set of <see cref="SettingDefinition"/>s used for discovery (Studio, adapters) and for
/// reconstructing typed values from persisted identifiers.
/// </summary>
/// <remarks>
/// A catalog never contains two different definitions for the same <see cref="SettingId"/>. Registering a definition
/// equal to an existing one is a no-op; registering an incompatible one fails. There is no global mutable registry:
/// consumers compose their own catalog, typically starting from <see cref="Settings.Catalog"/>.
/// </remarks>
public sealed class SettingCatalog : IReadOnlyCollection<SettingDefinition>
{
    private readonly ImmutableArray<SettingDefinition> _ordered;
    private readonly ImmutableDictionary<SettingId, SettingDefinition> _byId;

    private SettingCatalog(ImmutableArray<SettingDefinition> ordered, ImmutableDictionary<SettingId, SettingDefinition> byId)
    {
        _ordered = ordered;
        _byId = byId;
    }

    /// <summary>The empty catalog.</summary>
    public static SettingCatalog Empty { get; } = new(ImmutableArray<SettingDefinition>.Empty, ImmutableDictionary<SettingId, SettingDefinition>.Empty);

    /// <inheritdoc />
    public int Count => _ordered.Length;

    /// <summary>Creates a catalog from definitions.</summary>
    /// <exception cref="ArgumentException">The same identifier is declared with different characteristics.</exception>
    public static SettingCatalog Create(params SettingDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return Create((IEnumerable<SettingDefinition>)definitions);
    }

    /// <summary>Creates a catalog from definitions.</summary>
    /// <exception cref="ArgumentException">The same identifier is declared with different characteristics.</exception>
    public static SettingCatalog Create(IEnumerable<SettingDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var catalog = Empty;
        foreach (var definition in definitions)
        {
            catalog = catalog.With(definition);
        }

        return catalog;
    }

    /// <summary>Returns a catalog that also contains <paramref name="definition"/>.</summary>
    /// <exception cref="ArgumentException">The identifier is already declared with different characteristics.</exception>
    public SettingCatalog With(SettingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (_byId.TryGetValue(definition.Id, out var existing))
        {
            return existing.Equals(definition)
                ? this
                : throw new ArgumentException($"Setting '{definition.Id}' is already declared with different characteristics (value type {existing.ValueType.Name}, scope policy {existing.ScopePolicy}, kind {existing.Kind}, label '{existing.DisplayName}').", nameof(definition));
        }

        return new SettingCatalog(_ordered.Add(definition), _byId.Add(definition.Id, definition));
    }

    /// <summary>Returns a catalog that also contains <paramref name="definitions"/>.</summary>
    /// <exception cref="ArgumentException">An identifier is already declared with different characteristics.</exception>
    public SettingCatalog With(IEnumerable<SettingDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var catalog = this;
        foreach (var definition in definitions)
        {
            catalog = catalog.With(definition);
        }

        return catalog;
    }

    /// <summary>Attempts to find the definition registered for <paramref name="id"/>.</summary>
    public bool TryGet(SettingId id, [MaybeNullWhen(false)] out SettingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _byId.TryGetValue(id, out definition);
    }

    /// <summary>Gets the definition registered for <paramref name="id"/>.</summary>
    /// <exception cref="KeyNotFoundException">No definition is registered for the identifier.</exception>
    public SettingDefinition Get(SettingId id)
        => TryGet(id, out var definition) ? definition : throw new KeyNotFoundException($"No setting '{id}' in the catalog.");

    /// <summary>Returns whether a definition is registered for <paramref name="id"/>.</summary>
    public bool Contains(SettingId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _byId.ContainsKey(id);
    }

    /// <summary>Returns whether <paramref name="definition"/> itself (not merely its identifier) is registered.</summary>
    public bool Contains(SettingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return _byId.TryGetValue(definition.Id, out var existing) && existing.Equals(definition);
    }

    /// <inheritdoc />
    public IEnumerator<SettingDefinition> GetEnumerator() => ((IEnumerable<SettingDefinition>)_ordered).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
