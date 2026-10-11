using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Tests;

public class ProfileDefinitionTests
{
    private static readonly ProfileId PalId = ProfileId.From("ld-pal");
    private static readonly ProfileId CinemaId = ProfileId.From("ld-pal-cinema");

    [Fact]
    public void Create_builds_a_root_profile_without_assignments()
    {
        var profile = ProfileDefinition.Create(PalId, ProfileName.From("LaserDisc PAL"));

        Assert.Equal(PalId, profile.Id);
        Assert.Equal("LaserDisc PAL", profile.Name.Value);
        Assert.Null(profile.Description);
        Assert.Empty(profile.Sources);
        Assert.Empty(profile.Settings);
    }

    [Fact]
    public void Fluent_operations_return_new_instances_and_never_mutate_the_receiver()
    {
        var pal = ProfileDefinition.Create(PalId, ProfileName.From("LaserDisc PAL"))
            .With(Settings.Input, VideoInput.FrontComposite)
            .With(Settings.OutputResolution, OutputResolution.FullHd50)
            .With(Settings.Hdr, HdrMode.Off);

        var cinema = ProfileDefinition.Create(CinemaId, ProfileName.From("PAL CinemaScope"))
            .BasedOn(pal.Id)
            .With(Settings.PictureFormat, PictureFormat.Letterboxed(AspectRatio.From(235, 100)));
        var renamed = cinema.Rename(ProfileName.From("PAL Scope"));
        var described = cinema.WithDescription("Letterboxed 2.35:1 films");
        var withoutFormat = cinema.Without(Settings.PictureFormat);

        Assert.Equal(3, pal.Settings.Count);
        Assert.Equal([pal.Id], cinema.Sources);
        Assert.Single(cinema.Settings);
        Assert.Equal("PAL CinemaScope", cinema.Name.Value);
        Assert.Equal("PAL Scope", renamed.Name.Value);
        Assert.Null(cinema.Description);
        Assert.Equal("Letterboxed 2.35:1 films", described.Description);
        Assert.Empty(withoutFormat.Settings);
        Assert.Single(cinema.Settings);
        Assert.NotSame(cinema, renamed);
    }

    [Fact]
    public void Description_is_trimmed_and_blank_text_becomes_null()
    {
        var profile = ProfileDefinition.Create(PalId, ProfileName.From("PAL"));

        Assert.Equal("Common PAL settings", profile.WithDescription("  Common PAL settings ").Description);
        Assert.Null(profile.WithDescription("   ").Description);
        Assert.Null(profile.WithDescription(null).Description);
    }

    [Fact]
    public void WithSources_keeps_order_and_rejects_duplicates_and_self_reference()
    {
        var epson = ProfileId.From("epson");
        var profile = ProfileDefinition.Create(CinemaId, ProfileName.From("Cinema"));

        var composed = profile.WithSources(PalId, epson);

        Assert.Equal([PalId, epson], composed.Sources);
        Assert.Empty(composed.WithoutSources().Sources);
        Assert.Throws<ArgumentException>(() => profile.WithSources(PalId, PalId));
        Assert.Throws<ArgumentException>(() => profile.WithSources(PalId, CinemaId));
        Assert.Throws<ArgumentException>(() => profile.BasedOn(CinemaId));
    }

    [Fact]
    public void BasedOn_replaces_the_sources_with_a_single_one()
    {
        var profile = ProfileDefinition.Create(CinemaId, ProfileName.From("Cinema")).WithSources(ProfileId.From("a"), ProfileId.From("b"));

        Assert.Equal([PalId], profile.BasedOn(PalId).Sources);
    }

    [Fact]
    public void Create_from_components_rebuilds_an_equivalent_definition()
    {
        var fluent = ProfileDefinition.Create(CinemaId, ProfileName.From("Cinema"))
            .WithDescription("desc")
            .BasedOn(PalId)
            .With(Settings.Hdr, HdrMode.Off)
            .With(Settings.ScalingMode, ScalingMode.AutoFill, SettingScope.For(VideoMode.Pal576i));

        var rebuilt = ProfileDefinition.Create(CinemaId, ProfileName.From("Cinema"), "desc", [PalId], ProfileSettings.From(fluent.Settings));

        Assert.True(fluent.ContentEquals(rebuilt));
        Assert.NotSame(fluent, rebuilt);
    }

    [Fact]
    public void ContentEquals_compares_declared_content_while_reference_equality_is_kept()
    {
        var a = ProfileDefinition.Create(PalId, ProfileName.From("PAL")).With(Settings.Hdr, HdrMode.Off);
        var same = ProfileDefinition.Create(PalId, ProfileName.From("PAL")).With(Settings.Hdr, HdrMode.Off);
        var otherValue = a.With(Settings.Hdr, HdrMode.Hlg);
        var otherName = a.Rename(ProfileName.From("PAL 2"));
        var otherSources = a.BasedOn(CinemaId);

        Assert.True(a.ContentEquals(same));
        Assert.False(a.Equals(same));
        Assert.False(a.ContentEquals(otherValue));
        Assert.False(a.ContentEquals(otherName));
        Assert.False(a.ContentEquals(otherSources));
        Assert.False(a.ContentEquals(null));
        Assert.Equal("ld-pal (PAL)", a.ToString());
    }

    [Fact]
    public void With_rejects_a_scope_that_the_setting_forbids()
    {
        var profile = ProfileDefinition.Create(PalId, ProfileName.From("PAL"));

        Assert.Throws<ArgumentException>(() => profile.With(Settings.OutputResolution, OutputResolution.FullHd50, SettingScope.For(VideoMode.Pal576i)));
    }
}
