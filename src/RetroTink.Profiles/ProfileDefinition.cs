using System.Collections.Immutable;

namespace RetroTink.Profiles;

/// <summary>
/// Declarative, possibly partial, immutable definition of a profile: an identity, a display name, an optional
/// description, an ordered list of source profiles and the <b>local</b> assignments.
/// </summary>
/// <remarks>
/// <para>All <c>With…</c> methods return a new instance; the receiver is never modified.</para>
/// <para>Identity is <see cref="Id"/>. Reference equality is kept on purpose: use <see cref="ContentEquals"/> to compare
/// the declared content of two definitions.</para>
/// <para>Sources are applied from first to last, the last one taking precedence; local assignments always take
/// precedence over sources. See <see cref="Composition.ProfileResolver"/>.</para>
/// </remarks>
public sealed class ProfileDefinition
{
    private readonly ImmutableArray<ProfileId> _sources;

    private ProfileDefinition(ProfileId id, ProfileName name, string? description, ImmutableArray<ProfileId> sources, ProfileSettings settings)
    {
        Id = id;
        Name = name;
        Description = description;
        _sources = sources;
        Settings = settings;
    }

    /// <summary>Identity of the profile.</summary>
    public ProfileId Id { get; }

    /// <summary>Display name.</summary>
    public ProfileName Name { get; }

    /// <summary>Optional free text, without any effect on resolution.</summary>
    public string? Description { get; }

    /// <summary>Ordered references to source profiles; empty for a root profile.</summary>
    public IReadOnlyList<ProfileId> Sources => _sources;

    /// <summary>Local assignments only; inherited values are not part of a definition.</summary>
    public ProfileSettings Settings { get; }

    /// <summary>Creates a root profile without assignments.</summary>
    public static ProfileDefinition Create(ProfileId id, ProfileName name)
        => Create(id, name, null, [], ProfileSettings.Empty);

    /// <summary>
    /// Creates a profile from already validated components, for example when reconstructing a model from storage.
    /// </summary>
    /// <exception cref="ArgumentException">A source is duplicated or references the profile itself.</exception>
    public static ProfileDefinition Create(ProfileId id, ProfileName name, string? description, IEnumerable<ProfileId> sources, ProfileSettings settings)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(settings);
        return new ProfileDefinition(id, name, NormalizeDescription(description), ValidateSources(id, sources), settings);
    }

    /// <summary>Returns a copy with another display name.</summary>
    public ProfileDefinition Rename(ProfileName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new ProfileDefinition(Id, name, Description, _sources, Settings);
    }

    /// <summary>Returns a copy with another description. Blank text is normalised to <see langword="null"/>.</summary>
    public ProfileDefinition WithDescription(string? description)
        => new(Id, Name, NormalizeDescription(description), _sources, Settings);

    /// <summary>Returns a copy whose single source is <paramref name="source"/>.</summary>
    public ProfileDefinition BasedOn(ProfileId source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return WithSources([source]);
    }

    /// <summary>Returns a copy with the given ordered sources (the last one takes precedence).</summary>
    /// <exception cref="ArgumentException">A source is duplicated or references the profile itself.</exception>
    public ProfileDefinition WithSources(params ProfileId[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        return WithSources((IEnumerable<ProfileId>)sources);
    }

    /// <summary>Returns a copy with the given ordered sources (the last one takes precedence).</summary>
    /// <exception cref="ArgumentException">A source is duplicated or references the profile itself.</exception>
    public ProfileDefinition WithSources(IEnumerable<ProfileId> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        return new ProfileDefinition(Id, Name, Description, ValidateSources(Id, sources), Settings);
    }

    /// <summary>Returns a copy without any source.</summary>
    public ProfileDefinition WithoutSources() => new(Id, Name, Description, [], Settings);

    /// <summary>Returns a copy where <paramref name="definition"/> is locally assigned <paramref name="value"/> in the global scope.</summary>
    public ProfileDefinition With<TValue>(SettingDefinition<TValue> definition, TValue value)
        where TValue : notnull, IEquatable<TValue>
        => WithSettings(Settings.With(definition, value));

    /// <summary>Returns a copy where <paramref name="definition"/> is locally assigned <paramref name="value"/> in <paramref name="scope"/>.</summary>
    public ProfileDefinition With<TValue>(SettingDefinition<TValue> definition, TValue value, SettingScope scope)
        where TValue : notnull, IEquatable<TValue>
        => WithSettings(Settings.With(definition, value, scope));

    /// <summary>Returns a copy without the local global assignment of <paramref name="definition"/>.</summary>
    public ProfileDefinition Without(SettingDefinition definition) => WithSettings(Settings.Without(definition));

    /// <summary>Returns a copy without the local assignment of <paramref name="definition"/> in <paramref name="scope"/>.</summary>
    public ProfileDefinition Without(SettingDefinition definition, SettingScope scope) => WithSettings(Settings.Without(definition, scope));

    /// <summary>Returns a copy whose local assignments are exactly <paramref name="settings"/>.</summary>
    public ProfileDefinition WithSettings(ProfileSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new ProfileDefinition(Id, Name, Description, _sources, settings);
    }

    /// <summary>
    /// Compares the declared content (identity, name, description, ordered sources and local assignments).
    /// </summary>
    public bool ContentEquals(ProfileDefinition? other)
        => other is not null
           && Id.Equals(other.Id)
           && Name.Equals(other.Name)
           && string.Equals(Description, other.Description, StringComparison.Ordinal)
           && _sources.SequenceEqual(other._sources)
           && Settings.Equals(other.Settings);

    /// <inheritdoc />
    public override string ToString() => $"{Id} ({Name})";

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static ImmutableArray<ProfileId> ValidateSources(ProfileId id, IEnumerable<ProfileId> sources)
    {
        var builder = ImmutableArray.CreateBuilder<ProfileId>();
        foreach (var source in sources)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(sources));
            if (source.Equals(id))
            {
                throw new ArgumentException($"Profile '{id}' cannot use itself as a source.", nameof(sources));
            }

            if (builder.Contains(source))
            {
                throw new ArgumentException($"Source '{source}' is declared more than once for profile '{id}'.", nameof(sources));
            }

            builder.Add(source);
        }

        return builder.ToImmutable();
    }
}
