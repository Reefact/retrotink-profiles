using System.Collections.Immutable;
using RetroTink.Profiles.Values;

namespace RetroTink.Profiles.Composition;

/// <summary>Explains where the effective value of a setting comes from, and which candidates it overrode.</summary>
public sealed class SettingProvenance
{
    internal SettingProvenance(SettingDefinition definition, VideoMode? context, ProfileId resolvedProfile, SettingCandidate effective, ImmutableArray<SettingCandidate> overridden)
    {
        Definition = definition;
        Context = context;
        Effective = effective;
        IsInherited = !effective.Source.Equals(resolvedProfile);
        Overridden = overridden;
    }

    /// <summary>The setting.</summary>
    public SettingDefinition Definition { get; }

    /// <summary>The video mode the lookup was made for, or <see langword="null"/> for a context-free (global) lookup.</summary>
    public VideoMode? Context { get; }

    /// <summary>The winning candidate.</summary>
    public SettingCandidate Effective { get; }

    /// <summary>The scope of the winning assignment (global, or the requested video mode).</summary>
    public SettingScope Scope => Effective.Assignment.Scope;

    /// <summary><see langword="true"/> when the value comes from a source rather than from the resolved profile itself.</summary>
    public bool IsInherited { get; }

    /// <summary>Candidates that lost, from the strongest to the weakest.</summary>
    public IReadOnlyList<SettingCandidate> Overridden { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Definition.Id}: {Effective}";
}
