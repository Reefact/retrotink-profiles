using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RetroTink.Profiles;

/// <summary>
/// Immutable set of <b>local</b> setting assignments, keyed by <c>(SettingId, SettingScope)</c>.
/// </summary>
/// <remarks>
/// <para>Absence of an assignment means "not defined here / inheritable". It is never represented by a value such as
/// <c>Auto</c>, <c>Off</c> or <see langword="null"/>.</para>
/// <para>Enumeration preserves insertion order (a replacement keeps the position of the entry it replaces), which
/// gives serialisers a deterministic output. Equality is structural and order-insensitive.</para>
/// <para>This type only holds local assignments. The effective, inherited view of a profile is
/// <see cref="Composition.ResolvedSettings"/>, whose lookup rules differ.</para>
/// </remarks>
public sealed class ProfileSettings : IEquatable<ProfileSettings>, IReadOnlyCollection<SettingAssignment>
{
    private readonly ImmutableArray<SettingAssignment> _assignments;

    private ProfileSettings(ImmutableArray<SettingAssignment> assignments) => _assignments = assignments;

    /// <summary>The empty set.</summary>
    public static ProfileSettings Empty { get; } = new(ImmutableArray<SettingAssignment>.Empty);

    /// <inheritdoc />
    public int Count => _assignments.Length;

    /// <summary>
    /// Builds a set from already validated assignments, for example when reconstructing a model from storage.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Two assignments share the same setting and scope, or the same setting identifier is used with different definitions.
    /// </exception>
    public static ProfileSettings From(IEnumerable<SettingAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        var result = Empty;
        foreach (var assignment in assignments)
        {
            ArgumentNullException.ThrowIfNull(assignment, nameof(assignments));
            if (result.Contains(assignment.SettingId, assignment.Scope))
            {
                throw new ArgumentException($"Duplicate assignment for setting '{assignment.SettingId}' in scope '{assignment.Scope}'.", nameof(assignments));
            }

            result = result.With(assignment);
        }

        return result;
    }

    /// <summary>Returns a set where <paramref name="definition"/> is assigned <paramref name="value"/> in the global scope.</summary>
    public ProfileSettings With<TValue>(SettingDefinition<TValue> definition, TValue value)
        where TValue : notnull, IEquatable<TValue>
        => With(new SettingAssignment<TValue>(definition, value, SettingScope.Global));

    /// <summary>Returns a set where <paramref name="definition"/> is assigned <paramref name="value"/> in <paramref name="scope"/>.</summary>
    /// <exception cref="ArgumentException">The scope is not admissible for the definition.</exception>
    public ProfileSettings With<TValue>(SettingDefinition<TValue> definition, TValue value, SettingScope scope)
        where TValue : notnull, IEquatable<TValue>
        => With(new SettingAssignment<TValue>(definition, value, scope));

    /// <summary>
    /// Returns a set containing <paramref name="assignment"/>, replacing any existing assignment with the same
    /// setting and scope. The current instance is left untouched.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The set already uses the same setting identifier with a different definition.
    /// </exception>
    public ProfileSettings With(SettingAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        EnsureCompatibleDefinition(assignment.Definition);

        var index = IndexOf(assignment.SettingId, assignment.Scope);
        return index >= 0
            ? new ProfileSettings(_assignments.SetItem(index, assignment))
            : new ProfileSettings(_assignments.Add(assignment));
    }

    /// <summary>Returns a set without the global assignment of <paramref name="definition"/>, if any.</summary>
    public ProfileSettings Without(SettingDefinition definition) => Without(definition, SettingScope.Global);

    /// <summary>
    /// Returns a set without the assignment of <paramref name="definition"/> in <paramref name="scope"/>, if any.
    /// Removing a local assignment does not set any value: an inherited or global value may apply again.
    /// </summary>
    public ProfileSettings Without(SettingDefinition definition, SettingScope scope)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(scope);
        var index = IndexOf(definition.Id, scope);
        return index >= 0 ? new ProfileSettings(_assignments.RemoveAt(index)) : this;
    }

    /// <summary>Gets the global value of <paramref name="definition"/>.</summary>
    /// <exception cref="KeyNotFoundException">No such local assignment.</exception>
    public TValue Get<TValue>(SettingDefinition<TValue> definition)
        where TValue : notnull, IEquatable<TValue>
        => Get(definition, SettingScope.Global);

    /// <summary>Gets the value of <paramref name="definition"/> in exactly <paramref name="scope"/>.</summary>
    /// <exception cref="KeyNotFoundException">No such local assignment.</exception>
    public TValue Get<TValue>(SettingDefinition<TValue> definition, SettingScope scope)
        where TValue : notnull, IEquatable<TValue>
        => TryGet(definition, scope, out var value)
            ? value
            : throw new KeyNotFoundException($"Setting '{definition?.Id}' has no local assignment in scope '{scope}'.");

    /// <summary>Attempts to get the global value of <paramref name="definition"/>.</summary>
    public bool TryGet<TValue>(SettingDefinition<TValue> definition, [MaybeNullWhen(false)] out TValue value)
        where TValue : notnull, IEquatable<TValue>
        => TryGet(definition, SettingScope.Global, out value);

    /// <summary>Attempts to get the value of <paramref name="definition"/> in exactly <paramref name="scope"/>.</summary>
    /// <exception cref="InvalidOperationException">The set uses the same setting identifier with a different definition.</exception>
    public bool TryGet<TValue>(SettingDefinition<TValue> definition, SettingScope scope, [MaybeNullWhen(false)] out TValue value)
        where TValue : notnull, IEquatable<TValue>
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(scope);
        if (TryGet(definition.Id, scope, out var assignment))
        {
            if (!assignment.Definition.Equals(definition))
            {
                throw new InvalidOperationException($"Setting '{definition.Id}' is assigned with a different definition (value type {assignment.Definition.ValueType.Name}) than the one requested ({definition.ValueType.Name}).");
            }

            value = ((SettingAssignment<TValue>)assignment).Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Attempts to get the assignment for <paramref name="id"/> in exactly <paramref name="scope"/>.</summary>
    public bool TryGet(SettingId id, SettingScope scope, [MaybeNullWhen(false)] out SettingAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(scope);
        var index = IndexOf(id, scope);
        assignment = index >= 0 ? _assignments[index] : null;
        return index >= 0;
    }

    /// <summary>Returns whether <paramref name="definition"/> is assigned in the global scope.</summary>
    public bool Contains(SettingDefinition definition) => Contains(definition, SettingScope.Global);

    /// <summary>Returns whether <paramref name="definition"/> is assigned in exactly <paramref name="scope"/>.</summary>
    public bool Contains(SettingDefinition definition, SettingScope scope)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Contains(definition.Id, scope);
    }

    /// <summary>Returns whether <paramref name="id"/> is assigned in exactly <paramref name="scope"/>.</summary>
    public bool Contains(SettingId id, SettingScope scope)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(scope);
        return IndexOf(id, scope) >= 0;
    }

    /// <summary>Returns whether <paramref name="id"/> is assigned in any scope.</summary>
    public bool ContainsAnyScope(SettingId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        foreach (var assignment in _assignments)
        {
            if (assignment.SettingId.Equals(id))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public IEnumerator<SettingAssignment> GetEnumerator() => ((IEnumerable<SettingAssignment>)_assignments).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Structural, order-insensitive equality.</summary>
    public bool Equals(ProfileSettings? other)
    {
        if (other is null || other.Count != Count)
        {
            return false;
        }

        foreach (var assignment in _assignments)
        {
            var index = other.IndexOf(assignment.SettingId, assignment.Scope);
            if (index < 0 || !other._assignments[index].Equals(assignment))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ProfileSettings);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Order-insensitive combination.
        var hash = 0;
        foreach (var assignment in _assignments)
        {
            hash ^= assignment.GetHashCode();
        }

        return HashCode.Combine(Count, hash);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Count} local assignment(s)";

    private int IndexOf(SettingId id, SettingScope scope)
    {
        for (var i = 0; i < _assignments.Length; i++)
        {
            var candidate = _assignments[i];
            if (candidate.SettingId.Equals(id) && candidate.Scope.Equals(scope))
            {
                return i;
            }
        }

        return -1;
    }

    private void EnsureCompatibleDefinition(SettingDefinition definition)
    {
        foreach (var existing in _assignments)
        {
            if (existing.SettingId.Equals(definition.Id) && !existing.Definition.Equals(definition))
            {
                throw new ArgumentException($"Setting '{definition.Id}' is already assigned with a different definition (value type {existing.Definition.ValueType.Name} vs {definition.ValueType.Name}).", nameof(definition));
            }
        }
    }
}
