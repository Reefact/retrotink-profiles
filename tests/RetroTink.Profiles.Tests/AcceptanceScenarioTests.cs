using RetroTink.Profiles.Composition;
using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Tests;

/// <summary>End-to-end scenario of the specification (§10): LaserDisc profiles with inheritance.</summary>
public class AcceptanceScenarioTests
{
    private static readonly ProfileId LdPal = ProfileId.From("ld-pal");
    private static readonly ProfileId LdPal43 = ProfileId.From("ld-pal-43");
    private static readonly ProfileId LdPalCinema = ProfileId.From("ld-pal-cinema");
    private static readonly ProfileId LdNtsc = ProfileId.From("ld-ntsc");
    private static readonly ProfileId Epson = ProfileId.From("epson-fullhd");

    public static IReadOnlyList<ProfileDefinition> Library() =>
    [
        ProfileDefinition.Create(LdPal, ProfileName.From("LaserDisc PAL"))
            .WithDescription("Common settings for PAL LaserDiscs")
            .With(Settings.Input, VideoInput.FrontComposite)
            .With(Settings.OutputResolution, OutputResolution.FullHd50)
            .With(Settings.Hdr, HdrMode.Off)
            .With(Settings.Telecine, TelecineMode.Cadence22, SettingScope.For(VideoMode.Pal576i)),
        ProfileDefinition.Create(LdPal43, ProfileName.From("PAL 4:3"))
            .BasedOn(LdPal)
            .With(Settings.AspectRatio, AspectRatio.FourThree)
            .With(Settings.PictureFormat, PictureFormat.FullFrame),
        ProfileDefinition.Create(LdPalCinema, ProfileName.From("PAL CinemaScope"))
            .BasedOn(LdPal)
            .With(Settings.PictureFormat, PictureFormat.Letterboxed(AspectRatio.From(235, 100))),
        ProfileDefinition.Create(LdNtsc, ProfileName.From("LaserDisc NTSC"))
            .With(Settings.Input, VideoInput.FrontComposite)
            .With(Settings.OutputResolution, OutputResolution.FullHd60)
            .With(Settings.Telecine, TelecineMode.Cadence32, SettingScope.For(VideoMode.Ntsc480i)),
        ProfileDefinition.Create(Epson, ProfileName.From("Epson EH-TW4400 (Full HD)"))
            .With(Settings.OutputResolution, OutputResolution.FullHd60)
            .With(Settings.Colorimetry, Colorimetry.Rec709)
            .With(Settings.RgbRange, RgbRange.Limited),
    ];

    [Fact]
    public void Variants_share_the_common_PAL_settings_and_adapt_their_geometry()
    {
        var resolver = new ProfileResolver(Library());

        var fourThree = resolver.Resolve(LdPal43).Profile;
        var cinema = resolver.Resolve(LdPalCinema).Profile;

        foreach (var variant in new[] { fourThree, cinema })
        {
            Assert.Equal(VideoInput.FrontComposite, variant.Settings.Get(Settings.Input));
            Assert.Equal(OutputResolution.FullHd50, variant.Settings.Get(Settings.OutputResolution));
            Assert.Equal(HdrMode.Off, variant.Settings.Get(Settings.Hdr));
            Assert.Equal(TelecineMode.Cadence22, variant.Settings.Get(Settings.Telecine, VideoMode.Pal576i));
            Assert.False(variant.Settings.TryGet(Settings.Telecine, out _));
        }

        Assert.Equal(PictureFormat.FullFrame, fourThree.Settings.Get(Settings.PictureFormat));
        Assert.Equal(AspectRatio.FourThree, fourThree.Settings.Get(Settings.AspectRatio));
        Assert.Equal(PictureFormat.Letterboxed(AspectRatio.From(47, 20)), cinema.Settings.Get(Settings.PictureFormat));
        Assert.False(cinema.Settings.TryGet(Settings.AspectRatio, out _));
    }

    [Fact]
    public void Composing_a_source_profile_with_a_display_profile_merges_both_and_reports_the_disagreement()
    {
        var combined = ProfileDefinition.Create(ProfileId.From("ld-pal-cinema-epson"), ProfileName.From("PAL CinemaScope on Epson"))
            .WithSources(LdPalCinema, Epson)
            .With(Settings.OutputResolution, OutputResolution.FullHd50);
        var resolver = new ProfileResolver([.. Library(), combined]);

        var result = resolver.Resolve(combined.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputResolution.FullHd50, result.Profile.Settings.Get(Settings.OutputResolution));
        Assert.Equal(Colorimetry.Rec709, result.Profile.Settings.Get(Settings.Colorimetry));
        Assert.Equal(PictureFormat.Letterboxed(AspectRatio.From(235, 100)), result.Profile.Settings.Get(Settings.PictureFormat));
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(ResolutionDiagnosticCodes.SourceConflict, warning.Code);
        Assert.Equal(Settings.OutputResolution.Id, warning.Setting);
        Assert.Equal([LdPal, Epson], warning.Candidates.Select(c => c.Source));
    }

    [Fact]
    public void Fixing_the_parent_benefits_the_variants_on_their_next_resolution_without_altering_earlier_results()
    {
        var library = Library();
        var before = new ProfileResolver(library).Resolve(LdPalCinema).Profile;

        var updated = library.Select(p => p.Id.Equals(LdPal) ? p.With(Settings.Hdr, HdrMode.Hlg).With(Settings.Deinterlacing, DeinterlacingMode.MotionAdaptive) : p).ToList();
        var after = new ProfileResolver(updated).Resolve(LdPalCinema).Profile;

        Assert.Equal(HdrMode.Off, before.Settings.Get(Settings.Hdr));
        Assert.False(before.Settings.TryGet(Settings.Deinterlacing, out _));
        Assert.Equal(HdrMode.Hlg, after.Settings.Get(Settings.Hdr));
        Assert.Equal(DeinterlacingMode.MotionAdaptive, after.Settings.Get(Settings.Deinterlacing));
        Assert.Equal(PictureFormat.Letterboxed(AspectRatio.From(235, 100)), after.Settings.Get(Settings.PictureFormat));
    }

    [Fact]
    public void A_local_override_in_a_variant_shields_it_from_the_parent_change()
    {
        var library = Library().Select(p => p.Id.Equals(LdPal43) ? p.With(Settings.Hdr, HdrMode.Off) : p).ToList();
        var updated = library.Select(p => p.Id.Equals(LdPal) ? p.With(Settings.Hdr, HdrMode.Hlg) : p).ToList();

        var fourThree = new ProfileResolver(updated).Resolve(LdPal43).Profile;
        var cinema = new ProfileResolver(updated).Resolve(LdPalCinema).Profile;

        Assert.Equal(HdrMode.Off, fourThree.Settings.Get(Settings.Hdr));
        Assert.Equal(HdrMode.Hlg, cinema.Settings.Get(Settings.Hdr));
    }
}
