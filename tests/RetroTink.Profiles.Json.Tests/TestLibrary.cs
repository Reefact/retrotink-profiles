using RetroTink.Profiles.Values;

namespace RetroTink.Profiles.Json.Tests;

internal static class TestLibrary
{
    public static readonly ProfileId LdPal = ProfileId.From("ld-pal");
    public static readonly ProfileId LdPal43 = ProfileId.From("ld-pal-43");
    public static readonly ProfileId LdPalCinema = ProfileId.From("ld-pal-cinema");
    public static readonly ProfileId LdNtsc = ProfileId.From("ld-ntsc");
    public static readonly ProfileId Epson = ProfileId.From("epson-fullhd");

    /// <summary>The same library as <c>Fixtures/laserdisc-v1.json</c>.</summary>
    public static IReadOnlyList<ProfileDefinition> Profiles() =>
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

    public static string Fixture(string name)
    {
        using var stream = typeof(TestLibrary).Assembly.GetManifestResourceStream($"Fixtures/{name}")
            ?? throw new InvalidOperationException($"Missing embedded fixture '{name}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
