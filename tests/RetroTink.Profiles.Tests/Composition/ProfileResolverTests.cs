using RetroTink.Profiles.Composition;
using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Tests.Composition;

public class ProfileResolverTests
{
    private static readonly ProfileId PalId = ProfileId.From("ld-pal");
    private static readonly ProfileId EpsonId = ProfileId.From("epson");
    private static readonly ProfileId CinemaId = ProfileId.From("ld-pal-cinema");
    private static readonly AspectRatio Scope235 = AspectRatio.From(235, 100);

    private static readonly ProfileDefinition Pal = Profile("ld-pal")
        .With(Settings.Input, VideoInput.FrontComposite)
        .With(Settings.OutputResolution, OutputResolution.FullHd50)
        .With(Settings.Hdr, HdrMode.Off);

    private static readonly ProfileDefinition Epson = Profile("epson")
        .With(Settings.OutputResolution, OutputResolution.FullHd60)
        .With(Settings.Colorimetry, Colorimetry.Rec709);

    private static readonly ProfileDefinition Cinema = Profile("ld-pal-cinema", PalId, EpsonId)
        .With(Settings.OutputResolution, OutputResolution.FullHd50)
        .With(Settings.PictureFormat, PictureFormat.Letterboxed(Scope235));

    private static ProfileDefinition Profile(string id, params ProfileId[] sources)
        => ProfileDefinition.Create(ProfileId.From(id), ProfileName.From(id)).WithSources(sources);

    private static ResolvedProfile Resolve(ProfileId id, params ProfileDefinition[] definitions)
    {
        var result = new ProfileResolver(definitions).Resolve(id);
        Assert.True(result.IsSuccess, string.Join(" | ", result.Diagnostics));
        return result.Profile;
    }

    [Fact]
    public void A_root_profile_resolves_to_its_local_values_with_local_provenance()
    {
        var resolved = Resolve(PalId, Pal);

        Assert.Equal(OutputResolution.FullHd50, resolved.Settings.Get(Settings.OutputResolution));
        Assert.True(resolved.Settings.TryGetProvenance(Settings.OutputResolution, out var provenance));
        Assert.False(provenance.IsInherited);
        Assert.Equal(PalId, provenance.Effective.Source);
        Assert.Null(provenance.Effective.Via);
        Assert.Empty(provenance.Overridden);
        Assert.Equal([Pal], resolved.Layers);
        Assert.False(resolved.Settings.TryGet(Settings.Colorimetry, out _));
    }

    [Fact]
    public void A_child_inherits_from_its_single_parent()
    {
        var child = Profile("child", PalId).With(Settings.Colorimetry, Colorimetry.Rec709);

        var resolved = Resolve(child.Id, Pal, child);

        Assert.Equal(VideoInput.FrontComposite, resolved.Settings.Get(Settings.Input));
        Assert.True(resolved.Settings.TryGetProvenance(Settings.Input, out var provenance));
        Assert.True(provenance.IsInherited);
        Assert.Equal(PalId, provenance.Effective.Source);
        Assert.Equal(PalId, provenance.Effective.Via);
        Assert.Equal(Colorimetry.Rec709, resolved.Settings.Get(Settings.Colorimetry));
    }

    [Fact]
    public void The_specification_example_resolves_as_documented()
    {
        var result = new ProfileResolver([Pal, Epson, Cinema]).Resolve(CinemaId);

        Assert.True(result.IsSuccess);
        var settings = result.Profile.Settings;
        Assert.Equal(VideoInput.FrontComposite, settings.Get(Settings.Input));
        Assert.Equal(OutputResolution.FullHd50, settings.Get(Settings.OutputResolution));
        Assert.Equal(HdrMode.Off, settings.Get(Settings.Hdr));
        Assert.Equal(Colorimetry.Rec709, settings.Get(Settings.Colorimetry));
        Assert.Equal(PictureFormat.Letterboxed(Scope235), settings.Get(Settings.PictureFormat));

        Assert.Equal([Cinema, Epson, Pal], result.Profile.Layers);
        Assert.Equal(PalId, Origin(settings, Settings.Input));
        Assert.Equal(CinemaId, Origin(settings, Settings.OutputResolution));
        Assert.Equal(PalId, Origin(settings, Settings.Hdr));
        Assert.Equal(EpsonId, Origin(settings, Settings.Colorimetry));
        Assert.Equal(CinemaId, Origin(settings, Settings.PictureFormat));

        var conflict = Assert.Single(result.Diagnostics);
        Assert.Equal(ResolutionDiagnosticCodes.SourceConflict, conflict.Code);
        Assert.Equal(DiagnosticSeverity.Warning, conflict.Severity);
        Assert.Equal(CinemaId, conflict.Profile);
        Assert.Equal(Settings.OutputResolution.Id, conflict.Setting);
        Assert.Equal(SettingScope.Global, conflict.Scope);
        Assert.Equal([PalId, EpsonId], conflict.RelatedProfiles);
        Assert.Equal(2, conflict.Candidates.Count);
        Assert.Contains("overrides the setting locally", conflict.Message, StringComparison.Ordinal);
        Assert.Contains("'epson' takes precedence", conflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Among_several_sources_the_last_declared_wins_when_the_profile_is_silent()
    {
        var composed = Profile("composed", PalId, EpsonId);

        var resolved = Resolve(composed.Id, Pal, Epson, composed);

        Assert.Equal(OutputResolution.FullHd60, resolved.Settings.Get(Settings.OutputResolution));
        Assert.Equal(EpsonId, Origin(resolved.Settings, Settings.OutputResolution));

        var reversed = Profile("reversed", EpsonId, PalId);
        Assert.Equal(OutputResolution.FullHd50, Resolve(reversed.Id, Pal, Epson, reversed).Settings.Get(Settings.OutputResolution));
    }

    [Fact]
    public void Agreeing_sources_do_not_produce_conflicts()
    {
        var other = Profile("other").With(Settings.OutputResolution, OutputResolution.FullHd50).With(Settings.RgbRange, RgbRange.Limited);
        var composed = Profile("composed", PalId, other.Id);

        var result = new ProfileResolver([Pal, other, composed]).Resolve(composed.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(RgbRange.Limited, result.Profile.Settings.Get(Settings.RgbRange));
    }

    [Fact]
    public void Provenance_lists_overridden_candidates_from_the_strongest_to_the_weakest()
    {
        var resolved = Resolve(CinemaId, Pal, Epson, Cinema);

        Assert.True(resolved.Settings.TryGetProvenance(Settings.OutputResolution, out var provenance));
        Assert.Equal(CinemaId, provenance.Effective.Source);
        Assert.Equal(
            [(EpsonId, OutputResolution.FullHd60), (PalId, OutputResolution.FullHd50)],
            provenance.Overridden.Select(c => (c.Source, ((SettingAssignment<OutputResolution>)c.Assignment).Value)));
        Assert.Equal([EpsonId, PalId], provenance.Overridden.Select(c => c.Via));
    }

    [Fact]
    public void Specific_scope_beats_global_scope_at_the_same_level()
    {
        var profile = Profile("p")
            .With(Settings.ScalingMode, ScalingMode.Proportional)
            .With(Settings.ScalingMode, ScalingMode.AutoFill, SettingScope.For(VideoMode.Pal576i));

        var resolved = Resolve(profile.Id, profile);

        Assert.Equal(ScalingMode.AutoFill, resolved.Settings.Get(Settings.ScalingMode, VideoMode.Pal576i));
        Assert.Equal(ScalingMode.Proportional, resolved.Settings.Get(Settings.ScalingMode, VideoMode.Ntsc480i));
        Assert.Equal(ScalingMode.Proportional, resolved.Settings.Get(Settings.ScalingMode));
        Assert.True(resolved.Settings.TryGetProvenance(Settings.ScalingMode, VideoMode.Pal576i, out var provenance));
        Assert.Equal(SettingScope.For(VideoMode.Pal576i), provenance.Scope);
        Assert.Single(provenance.Overridden);
    }

    [Fact]
    public void Child_global_assignment_beats_parent_video_mode_assignment()
    {
        var parent = Profile("parent").With(Settings.ScalingMode, ScalingMode.AutoFill, SettingScope.For(VideoMode.Pal576i));
        var child = Profile("child", parent.Id).With(Settings.ScalingMode, ScalingMode.Proportional);

        var resolved = Resolve(child.Id, parent, child);

        Assert.Equal(ScalingMode.Proportional, resolved.Settings.Get(Settings.ScalingMode, VideoMode.Pal576i));
        Assert.True(resolved.Settings.TryGetProvenance(Settings.ScalingMode, VideoMode.Pal576i, out var provenance));
        Assert.Equal(child.Id, provenance.Effective.Source);
        Assert.Equal(SettingScope.Global, provenance.Scope);
        Assert.Equal(parent.Id, Assert.Single(provenance.Overridden).Source);
    }

    [Fact]
    public void Parent_video_mode_assignment_applies_when_the_child_is_silent()
    {
        var parent = Profile("parent").With(Settings.ScalingMode, ScalingMode.AutoFill, SettingScope.For(VideoMode.Pal576i));
        var child = Profile("child", parent.Id);

        var resolved = Resolve(child.Id, parent, child);

        Assert.Equal(ScalingMode.AutoFill, resolved.Settings.Get(Settings.ScalingMode, VideoMode.Pal576i));
        Assert.False(resolved.Settings.TryGet(Settings.ScalingMode, VideoMode.Ntsc480i, out _));
        Assert.False(resolved.Settings.TryGet(Settings.ScalingMode, out _), "a context-free lookup must not consult video-mode assignments");
        Assert.Throws<KeyNotFoundException>(() => resolved.Settings.Get(Settings.ScalingMode));
    }

    [Fact]
    public void Removing_a_local_override_makes_the_inherited_value_reappear()
    {
        var overriding = Profile("child", PalId, EpsonId).With(Settings.OutputResolution, OutputResolution.Uhd60);
        var inheriting = overriding.Without(Settings.OutputResolution);

        Assert.Equal(OutputResolution.Uhd60, Resolve(overriding.Id, Pal, Epson, overriding).Settings.Get(Settings.OutputResolution));
        Assert.Equal(OutputResolution.FullHd60, Resolve(inheriting.Id, Pal, Epson, inheriting).Settings.Get(Settings.OutputResolution));
    }

    [Fact]
    public void A_missing_source_fails_with_a_stable_code()
    {
        var orphan = Profile("orphan", ProfileId.From("nowhere"));

        var result = new ProfileResolver([orphan]).Resolve(orphan.Id);

        Assert.False(result.IsSuccess);
        Assert.False(result.TryGetProfile(out _));
        var error = Assert.Single(result.Errors);
        Assert.Equal(ResolutionDiagnosticCodes.MissingSource, error.Code);
        Assert.Equal(orphan.Id, error.Profile);
        Assert.Equal([ProfileId.From("nowhere")], error.RelatedProfiles);
        Assert.Throws<InvalidOperationException>(() => result.Profile);
    }

    [Fact]
    public void A_direct_cycle_fails_with_its_path()
    {
        var a = Profile("a", ProfileId.From("b"));
        var b = Profile("b", ProfileId.From("a"));

        var result = new ProfileResolver([a, b]).Resolve(a.Id);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(ResolutionDiagnosticCodes.CyclicSources, error.Code);
        Assert.Equal([a.Id, b.Id, a.Id], error.RelatedProfiles);
    }

    [Fact]
    public void An_indirect_cycle_fails_with_its_path()
    {
        var a = Profile("a", ProfileId.From("b"));
        var b = Profile("b", ProfileId.From("c"));
        var c = Profile("c", ProfileId.From("a"));
        var d = Profile("d", a.Id);

        var result = new ProfileResolver([a, b, c, d]).Resolve(d.Id);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(ResolutionDiagnosticCodes.CyclicSources, error.Code);
        Assert.Equal([a.Id, b.Id, c.Id, a.Id], error.RelatedProfiles);
    }

    [Fact]
    public void Duplicate_identifiers_fail_every_resolution()
    {
        var twin = Profile("ld-pal").With(Settings.Hdr, HdrMode.Hlg);

        var resolver = new ProfileResolver([Pal, Epson, twin]);
        var result = resolver.Resolve(EpsonId);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(ResolutionDiagnosticCodes.DuplicateProfileId, error.Code);
        Assert.Equal(PalId, error.Profile);
        Assert.Equal(3, resolver.Definitions.Count);
    }

    [Fact]
    public void An_unknown_profile_fails()
    {
        var result = new ProfileResolver([Pal]).Resolve(EpsonId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResolutionDiagnosticCodes.UnknownProfile, Assert.Single(result.Diagnostics).Code);
        Assert.Equal(EpsonId, result.Requested);
    }

    [Fact]
    public void Diamond_inheritance_is_not_a_cycle_and_keeps_a_shared_ancestor_at_its_strongest_position()
    {
        var a = Profile("a").With(Settings.Hdr, HdrMode.Off).With(Settings.Input, VideoInput.Scart);
        var b = Profile("b", a.Id).With(Settings.Colorimetry, Colorimetry.Rec601);
        var c = Profile("c", a.Id).With(Settings.Colorimetry, Colorimetry.Rec709);
        var d = Profile("d", b.Id, c.Id);

        var result = new ProfileResolver([a, b, c, d]).Resolve(d.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal([d, c, a, b], result.Profile.Layers);
        Assert.Equal(HdrMode.Off, result.Profile.Settings.Get(Settings.Hdr));
        Assert.True(result.Profile.Settings.TryGetProvenance(Settings.Hdr, out var hdr));
        Assert.Equal(a.Id, hdr.Effective.Source);
        Assert.Equal(c.Id, hdr.Effective.Via);
        Assert.Empty(hdr.Overridden);
        Assert.Equal(Colorimetry.Rec709, result.Profile.Settings.Get(Settings.Colorimetry));

        var conflict = Assert.Single(result.Diagnostics);
        Assert.Equal(ResolutionDiagnosticCodes.SourceConflict, conflict.Code);
        Assert.Equal(Settings.Colorimetry.Id, conflict.Setting);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Setting == Settings.Hdr.Id);
    }

    [Fact]
    public void Later_source_inheriting_a_shared_ancestor_beats_an_earlier_source_overriding_it()
    {
        // Documented semantics: a source contributes its *effective* configuration, and the last source wins.
        var a = Profile("a").With(Settings.OutputResolution, OutputResolution.FullHd60);
        var b = Profile("b", a.Id).With(Settings.OutputResolution, OutputResolution.FullHd50);
        var c = Profile("c", a.Id);
        var d = Profile("d", b.Id, c.Id);

        var result = new ProfileResolver([a, b, c, d]).Resolve(d.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputResolution.FullHd60, result.Profile.Settings.Get(Settings.OutputResolution));
        var conflict = Assert.Single(result.Diagnostics);
        Assert.Equal(ResolutionDiagnosticCodes.SourceConflict, conflict.Code);
        Assert.Equal([b.Id, c.Id], conflict.RelatedProfiles);
        Assert.Equal([b.Id, a.Id], conflict.Candidates.Select(candidate => candidate.Source));
        Assert.Contains("'c' takes precedence (value declared by 'a')", conflict.Message, StringComparison.Ordinal);

        var reordered = Profile("d2", c.Id, b.Id);
        Assert.Equal(OutputResolution.FullHd50, Resolve(reordered.Id, a, b, c, reordered).Settings.Get(Settings.OutputResolution));
    }

    [Fact]
    public void Conflicts_are_reported_for_intermediate_profiles_too()
    {
        var a = Profile("a").With(Settings.Hdr, HdrMode.Off);
        var b = Profile("b").With(Settings.Hdr, HdrMode.Hlg);
        var c = Profile("c", a.Id, b.Id);
        var d = Profile("d", c.Id);

        var result = new ProfileResolver([a, b, c, d]).Resolve(d.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(HdrMode.Hlg, result.Profile.Settings.Get(Settings.Hdr));
        var conflict = Assert.Single(result.Diagnostics);
        Assert.Equal(c.Id, conflict.Profile);
    }

    [Fact]
    public void Resolution_is_deterministic()
    {
        var first = new ProfileResolver([Pal, Epson, Cinema]).Resolve(CinemaId);
        var second = new ProfileResolver([Pal, Epson, Cinema]).Resolve(CinemaId);

        Assert.Equal(first.Diagnostics.Select(d => d.ToString()), second.Diagnostics.Select(d => d.ToString()));
        Assert.Equal(first.Profile.Settings.Flatten(), second.Profile.Settings.Flatten());
        Assert.Equal(first.Profile.Layers, second.Profile.Layers);
    }

    [Fact]
    public void Earlier_results_are_not_affected_by_later_definitions()
    {
        var before = Resolve(CinemaId, Pal, Epson, Cinema);

        var updatedPal = Pal.With(Settings.Hdr, HdrMode.Hlg);
        var after = Resolve(CinemaId, updatedPal, Epson, Cinema);

        Assert.Equal(HdrMode.Off, before.Settings.Get(Settings.Hdr));
        Assert.Equal(HdrMode.Hlg, after.Settings.Get(Settings.Hdr));
        Assert.Equal(HdrMode.Off, Pal.Settings.Get(Settings.Hdr));
    }

    [Fact]
    public void Incompatible_definitions_for_the_same_setting_fail()
    {
        var impostor = new SettingDefinition<Colorimetry>(Settings.Hdr.Id, "HDR as colorimetry");
        var child = Profile("child", PalId).With(impostor, Colorimetry.Rec709);

        var result = new ProfileResolver([Pal, child]).Resolve(child.Id);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(ResolutionDiagnosticCodes.IncompatibleSettingDefinitions, error.Code);
        Assert.Equal(Settings.Hdr.Id, error.Setting);
        Assert.Equal([child.Id, PalId], error.RelatedProfiles);
    }

    [Fact]
    public void Flatten_and_ListEffective_expose_the_effective_configuration()
    {
        var resolved = Resolve(CinemaId, Pal, Epson, Cinema);

        var flat = resolved.Settings.Flatten();
        var listed = resolved.Settings.ListEffective();

        Assert.Equal(5, flat.Count);
        Assert.Equal(Colorimetry.Rec709, flat.Get(Settings.Colorimetry));
        Assert.Equal(OutputResolution.FullHd50, flat.Get(Settings.OutputResolution));
        Assert.All(flat, assignment => Assert.True(assignment.Scope.IsGlobal));
        Assert.Equal(
            ["geometry.pictureFormat", "input.source", "output.colorimetry", "output.hdr", "output.resolution"],
            listed.Select(p => p.Definition.Id.Value));
        Assert.Equal(listed.Select(p => p.Definition), resolved.Settings.Definitions);
    }

    [Fact]
    public void Flatten_for_a_video_mode_applies_the_scope_rule()
    {
        var parent = Profile("parent")
            .With(Settings.ScalingMode, ScalingMode.AutoFill, SettingScope.For(VideoMode.Pal576i))
            .With(Settings.Deinterlacing, DeinterlacingMode.MotionAdaptive);
        var child = Profile("child", parent.Id).With(Settings.Deinterlacing, DeinterlacingMode.Bob, SettingScope.For(VideoMode.Pal576i));

        var resolved = Resolve(child.Id, parent, child);

        var pal = resolved.Settings.Flatten(VideoMode.Pal576i);
        var global = resolved.Settings.Flatten();
        Assert.Equal(ScalingMode.AutoFill, pal.Get(Settings.ScalingMode));
        Assert.Equal(DeinterlacingMode.Bob, pal.Get(Settings.Deinterlacing));
        Assert.Single(global);
        Assert.Equal(DeinterlacingMode.MotionAdaptive, global.Get(Settings.Deinterlacing));
    }

    private static ProfileId Origin(ResolvedSettings settings, SettingDefinition definition)
    {
        Assert.True(settings.TryGetProvenance(definition, out var provenance));
        return provenance.Effective.Source;
    }
}
