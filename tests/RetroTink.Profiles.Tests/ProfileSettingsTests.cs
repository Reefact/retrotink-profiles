using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Tests;

public class ProfileSettingsTests
{
    private static readonly SettingScope Pal = SettingScope.For(VideoMode.Pal576i);

    [Fact]
    public void Empty_has_no_assignment()
    {
        Assert.Empty(ProfileSettings.Empty);
        Assert.False(ProfileSettings.Empty.TryGet(Settings.Hdr, out _));
        Assert.Throws<KeyNotFoundException>(() => ProfileSettings.Empty.Get(Settings.Hdr));
    }

    [Fact]
    public void With_adds_a_typed_assignment_and_leaves_the_original_untouched()
    {
        var original = ProfileSettings.Empty;

        var updated = original.With(Settings.OutputResolution, OutputResolution.FullHd50);

        Assert.Empty(original);
        Assert.Single(updated);
        Assert.Equal(OutputResolution.FullHd50, updated.Get(Settings.OutputResolution));
        Assert.True(updated.Contains(Settings.OutputResolution));
        Assert.False(updated.Contains(Settings.Hdr));
    }

    [Fact]
    public void With_replaces_the_assignment_with_the_same_setting_and_scope_in_place()
    {
        var settings = ProfileSettings.Empty
            .With(Settings.Input, VideoInput.FrontComposite)
            .With(Settings.OutputResolution, OutputResolution.FullHd50)
            .With(Settings.Hdr, HdrMode.Off);

        var updated = settings.With(Settings.OutputResolution, OutputResolution.FullHd60);

        Assert.Equal(3, updated.Count);
        Assert.Equal(OutputResolution.FullHd60, updated.Get(Settings.OutputResolution));
        Assert.Equal(OutputResolution.FullHd50, settings.Get(Settings.OutputResolution));
        Assert.Equal([Settings.Input.Id, Settings.OutputResolution.Id, Settings.Hdr.Id], updated.Select(a => a.SettingId));
    }

    [Fact]
    public void Scoped_and_global_assignments_of_the_same_setting_coexist()
    {
        var settings = ProfileSettings.Empty
            .With(Settings.ScalingMode, ScalingMode.Proportional)
            .With(Settings.ScalingMode, ScalingMode.AutoFill, Pal);

        Assert.Equal(2, settings.Count);
        Assert.Equal(ScalingMode.Proportional, settings.Get(Settings.ScalingMode));
        Assert.Equal(ScalingMode.AutoFill, settings.Get(Settings.ScalingMode, Pal));
        Assert.True(settings.ContainsAnyScope(Settings.ScalingMode.Id));
    }

    [Fact]
    public void Without_removes_only_the_matching_scope()
    {
        var settings = ProfileSettings.Empty
            .With(Settings.ScalingMode, ScalingMode.Proportional)
            .With(Settings.ScalingMode, ScalingMode.AutoFill, Pal);

        var withoutScoped = settings.Without(Settings.ScalingMode, Pal);
        var withoutGlobal = settings.Without(Settings.ScalingMode);

        Assert.Equal(2, settings.Count);
        Assert.Single(withoutScoped);
        Assert.True(withoutScoped.Contains(Settings.ScalingMode));
        Assert.False(withoutScoped.Contains(Settings.ScalingMode, Pal));
        Assert.Single(withoutGlobal);
        Assert.False(withoutGlobal.Contains(Settings.ScalingMode));
        Assert.True(withoutGlobal.Contains(Settings.ScalingMode, Pal));
    }

    [Fact]
    public void Without_an_absent_assignment_returns_the_same_instance()
    {
        var settings = ProfileSettings.Empty.With(Settings.Hdr, HdrMode.Off);

        Assert.Same(settings, settings.Without(Settings.Input));
    }

    [Fact]
    public void Absence_differs_from_explicit_Auto_or_Off()
    {
        var absent = ProfileSettings.Empty;
        var explicitAuto = ProfileSettings.Empty.With(Settings.VideoStandard, VideoStandard.Auto);
        var explicitOff = ProfileSettings.Empty.With(Settings.Hdr, HdrMode.Off);

        Assert.False(absent.TryGet(Settings.VideoStandard, out _));
        Assert.True(explicitAuto.TryGet(Settings.VideoStandard, out var standard));
        Assert.Equal(VideoStandard.Auto, standard);
        Assert.True(explicitOff.TryGet(Settings.Hdr, out var hdr));
        Assert.Equal(HdrMode.Off, hdr);
        Assert.NotEqual(absent, explicitAuto);
    }

    [Fact]
    public void A_scope_that_the_definition_forbids_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => ProfileSettings.Empty.With(Settings.OutputResolution, OutputResolution.FullHd50, Pal));
    }

    [Fact]
    public void The_same_setting_id_cannot_be_used_with_a_different_definition()
    {
        var impostor = new SettingDefinition<Colorimetry>(Settings.Hdr.Id, "HDR as colorimetry");
        var settings = ProfileSettings.Empty.With(Settings.Hdr, HdrMode.Off);

        Assert.Throws<ArgumentException>(() => settings.With(impostor, Colorimetry.Rec709));
        Assert.Throws<InvalidOperationException>(() => settings.TryGet(impostor, out _));
    }

    [Fact]
    public void Equality_is_structural_and_order_insensitive()
    {
        var a = ProfileSettings.Empty.With(Settings.Hdr, HdrMode.Off).With(Settings.Input, VideoInput.FrontComposite);
        var b = ProfileSettings.Empty.With(Settings.Input, VideoInput.FrontComposite).With(Settings.Hdr, HdrMode.Off);
        var c = b.With(Settings.Hdr, HdrMode.Hlg);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.NotEqual(a, ProfileSettings.Empty);
    }

    [Fact]
    public void From_rebuilds_a_set_and_rejects_duplicates()
    {
        var assignments = new SettingAssignment[]
        {
            new SettingAssignment<HdrMode>(Settings.Hdr, HdrMode.Off),
            new SettingAssignment<ScalingMode>(Settings.ScalingMode, ScalingMode.AutoFill, Pal),
        };

        var settings = ProfileSettings.From(assignments);

        Assert.Equal(2, settings.Count);
        Assert.Equal(ScalingMode.AutoFill, settings.Get(Settings.ScalingMode, Pal));
        Assert.Throws<ArgumentException>(() => ProfileSettings.From([.. assignments, new SettingAssignment<HdrMode>(Settings.Hdr, HdrMode.Hlg)]));
    }

    [Fact]
    public void Enumeration_yields_typed_assignments_in_insertion_order()
    {
        var settings = ProfileSettings.Empty
            .With(Settings.Hdr, HdrMode.Off)
            .With(Settings.PictureFormat, PictureFormat.Letterboxed(AspectRatio.From(235, 100)));

        var assignments = settings.ToList();

        var hdr = Assert.IsType<SettingAssignment<HdrMode>>(assignments[0]);
        Assert.Equal(HdrMode.Off, hdr.Value);
        var format = Assert.IsType<SettingAssignment<PictureFormat>>(assignments[1]);
        Assert.Equal(AspectRatio.From(47, 20), format.Value.ContentAspectRatio);
        Assert.Equal("output.hdr = off", hdr.ToString());
    }
}
