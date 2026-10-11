namespace RetroTink.Profiles;

/// <summary>Type-erased view of a <see cref="SettingDefinition{TValue}"/>, for catalogs and generic tooling.</summary>
/// <remarks>
/// Two definitions are equal when all their declared characteristics are equal. A given <see cref="SettingId"/>
/// must always map to one definition: catalogs, <see cref="ProfileSettings"/> and the resolver reject the same
/// identifier declared with different characteristics.
/// </remarks>
public abstract class SettingDefinition : IEquatable<SettingDefinition>
{
    private protected SettingDefinition(SettingId id, string displayName, SettingScopePolicy scopePolicy, SettingKind kind)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (!Enum.IsDefined(scopePolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(scopePolicy), scopePolicy, "Unknown scope policy.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown setting kind.");
        }

        Id = id;
        DisplayName = displayName.Trim();
        ScopePolicy = scopePolicy;
        Kind = kind;
    }

    /// <summary>Stable identifier of the setting.</summary>
    public SettingId Id { get; }

    /// <summary>Default English label, for discovery and tooling. Localisation is the consumer's concern.</summary>
    public string DisplayName { get; }

    /// <summary>Which scopes are admissible for this setting.</summary>
    public SettingScopePolicy ScopePolicy { get; }

    /// <summary>Whether the setting is a concrete hardware setting or a high-level intention.</summary>
    public SettingKind Kind { get; }

    /// <summary>The .NET type of the values this setting accepts.</summary>
    public abstract Type ValueType { get; }

    /// <summary>Returns whether <paramref name="scope"/> is admissible for this setting.</summary>
    public bool AllowsScope(SettingScope scope) => ScopePolicy.Allows(scope);

    /// <summary>Dispatches to the typed overload of <paramref name="visitor"/> without casts on the caller side.</summary>
    public abstract TResult Accept<TResult>(ISettingDefinitionVisitor<TResult> visitor);

    /// <inheritdoc />
    public bool Equals(SettingDefinition? other) =>
        other is not null
        && Id.Equals(other.Id)
        && ValueType == other.ValueType
        && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
        && ScopePolicy == other.ScopePolicy
        && Kind == other.Kind;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SettingDefinition);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, ValueType, StringComparer.Ordinal.GetHashCode(DisplayName), ScopePolicy, Kind);

    /// <inheritdoc />
    public override string ToString() => Id.Value;
}

/// <summary>Definition of a known setting whose values are of type <typeparamref name="TValue"/>.</summary>
/// <typeparam name="TValue">An immutable value type with value equality (a Smart Enum or a Value Object).</typeparam>
/// <remarks>
/// A definition carries no binary offset, codec code, file path or firmware list: those belong to format adapters.
/// Intrinsic validity of a value belongs to the value type itself.
/// </remarks>
public sealed class SettingDefinition<TValue> : SettingDefinition
    where TValue : notnull, IEquatable<TValue>
{
    /// <summary>Creates a definition.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="displayName">Default English label.</param>
    /// <param name="scopePolicy">Admissible scopes; global only by default.</param>
    /// <param name="kind">Hardware setting (default) or high-level intention.</param>
    public SettingDefinition(SettingId id, string displayName, SettingScopePolicy scopePolicy = SettingScopePolicy.GlobalOnly, SettingKind kind = SettingKind.Hardware)
        : base(id, displayName, scopePolicy, kind)
    {
    }

    /// <inheritdoc />
    public override Type ValueType => typeof(TValue);

    /// <inheritdoc />
    public override TResult Accept<TResult>(ISettingDefinitionVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }
}

/// <summary>Typed double-dispatch over <see cref="SettingDefinition"/> instances.</summary>
/// <typeparam name="TResult">Result of the visit.</typeparam>
public interface ISettingDefinitionVisitor<out TResult>
{
    /// <summary>Visits a definition with its concrete value type.</summary>
    TResult Visit<TValue>(SettingDefinition<TValue> definition)
        where TValue : notnull, IEquatable<TValue>;
}
