namespace RetroTink.Profiles.Composition;

/// <summary>A value that competed for a setting during resolution, with the definition that declared it.</summary>
public sealed class SettingCandidate
{
    internal SettingCandidate(ProfileId source, ProfileId? via, SettingAssignment assignment)
    {
        Source = source;
        Via = via;
        Assignment = assignment;
    }

    /// <summary>The definition that actually declares the assignment (the ultimate origin, never an intermediate).</summary>
    public ProfileId Source { get; }

    /// <summary>
    /// The direct source of the resolved profile through which this candidate was reached, or <see langword="null"/>
    /// when the candidate is a local assignment of the resolved profile itself.
    /// </summary>
    public ProfileId? Via { get; }

    /// <summary>The assignment (definition, scope and value).</summary>
    public SettingAssignment Assignment { get; }

    /// <inheritdoc />
    public override string ToString() => Via is null || Via.Equals(Source) ? $"{Assignment} from '{Source}'" : $"{Assignment} from '{Source}' via '{Via}'";
}
