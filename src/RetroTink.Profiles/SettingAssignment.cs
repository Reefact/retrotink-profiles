namespace RetroTink.Profiles;

/// <summary>Type-erased view of a local assignment: a definition, a scope and a value.</summary>
public abstract class SettingAssignment : IEquatable<SettingAssignment>
{
    private protected SettingAssignment(SettingScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        Scope = scope;
    }

    /// <summary>The setting being assigned.</summary>
    public abstract SettingDefinition Definition { get; }

    /// <summary>The scope of the assignment.</summary>
    public SettingScope Scope { get; }

    /// <summary>Shortcut for <c>Definition.Id</c>.</summary>
    public SettingId SettingId => Definition.Id;

    /// <summary>Dispatches to the typed overload of <paramref name="visitor"/> without casts on the caller side.</summary>
    public abstract TResult Accept<TResult>(ISettingAssignmentVisitor<TResult> visitor);

    /// <summary>Textual form of the value, for diagnostics and display.</summary>
    public abstract string FormatValue();

    /// <inheritdoc />
    public abstract bool Equals(SettingAssignment? other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SettingAssignment);

    /// <inheritdoc />
    public abstract override int GetHashCode();
}

/// <summary>A local assignment of a <typeparamref name="TValue"/> to a setting, in a given scope.</summary>
public sealed class SettingAssignment<TValue> : SettingAssignment
    where TValue : notnull, IEquatable<TValue>
{
    /// <summary>Creates an assignment.</summary>
    /// <exception cref="ArgumentException">The scope is not admissible for the definition.</exception>
    public SettingAssignment(SettingDefinition<TValue> definition, TValue value, SettingScope scope)
        : base(scope)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(value);
        if (!definition.AllowsScope(scope))
        {
            throw new ArgumentException($"Scope '{scope}' is not admissible for setting '{definition.Id}' (policy: {definition.ScopePolicy}).", nameof(scope));
        }

        Definition = definition;
        Value = value;
    }

    /// <summary>Creates a global assignment.</summary>
    public SettingAssignment(SettingDefinition<TValue> definition, TValue value)
        : this(definition, value, SettingScope.Global)
    {
    }

    /// <inheritdoc />
    public override SettingDefinition<TValue> Definition { get; }

    /// <summary>The assigned value.</summary>
    public TValue Value { get; }

    /// <inheritdoc />
    public override TResult Accept<TResult>(ISettingAssignmentVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string FormatValue() => Value.ToString() ?? string.Empty;

    /// <inheritdoc />
    public override bool Equals(SettingAssignment? other) =>
        other is SettingAssignment<TValue> typed
        && Definition.Equals(typed.Definition)
        && Scope.Equals(typed.Scope)
        && Value.Equals(typed.Value);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Definition, Scope, Value);

    /// <inheritdoc />
    public override string ToString() => Scope.IsGlobal ? $"{Definition.Id} = {FormatValue()}" : $"{Definition.Id} [{Scope}] = {FormatValue()}";
}

/// <summary>Typed double-dispatch over <see cref="SettingAssignment"/> instances.</summary>
/// <typeparam name="TResult">Result of the visit.</typeparam>
public interface ISettingAssignmentVisitor<out TResult>
{
    /// <summary>Visits an assignment with its concrete value type.</summary>
    TResult Visit<TValue>(SettingAssignment<TValue> assignment)
        where TValue : notnull, IEquatable<TValue>;
}
