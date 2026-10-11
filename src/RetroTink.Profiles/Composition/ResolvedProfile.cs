using System.Collections.Immutable;

namespace RetroTink.Profiles.Composition;

/// <summary>
/// Autonomous, immutable, effective configuration of a profile. It keeps referencing the definitions it was computed
/// from, so later changes (which produce new definitions) never alter it.
/// </summary>
/// <remarks>It is not necessarily complete for a hardware export: a setting may simply be unassigned.</remarks>
public sealed class ResolvedProfile
{
    internal ResolvedProfile(ProfileDefinition definition, ImmutableArray<ProfileResolver.Layer> layers)
    {
        Definition = definition;
        Layers = [.. layers.Select(l => l.Definition)];
        Settings = new ResolvedSettings(definition.Id, layers);
    }

    /// <summary>Identity of the resolved profile.</summary>
    public ProfileId Id => Definition.Id;

    /// <summary>Display name of the resolved profile.</summary>
    public ProfileName Name => Definition.Name;

    /// <summary>Description of the resolved profile.</summary>
    public string? Description => Definition.Description;

    /// <summary>The definition that was resolved.</summary>
    public ProfileDefinition Definition { get; }

    /// <summary>
    /// The definitions contributing to the configuration, in priority order: the profile itself first, then its sources
    /// from the last declared to the first, each immediately followed by its own ancestors. A definition reachable through
    /// several paths appears once, at its strongest position.
    /// </summary>
    public IReadOnlyList<ProfileDefinition> Layers { get; }

    /// <summary>Effective settings with provenance.</summary>
    public ResolvedSettings Settings { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Id} (resolved from {Layers.Count} layer(s))";
}
