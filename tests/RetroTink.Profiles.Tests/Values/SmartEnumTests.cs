using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Tests.Values;

public class SmartEnumTests
{
    [Fact]
    public void Members_are_exposed_from_their_own_class_and_listed_in_All()
    {
        Assert.Contains(HdrMode.Off, HdrMode.All);
        Assert.Contains(VideoInput.FrontComposite, VideoInput.All);
        Assert.Contains(OutputResolution.FullHd50, OutputResolution.All);
    }

    [Fact]
    public void TryFromKey_finds_members_by_their_stable_key()
    {
        Assert.True(OutputResolution.TryFromKey("1080p50", out var resolution));
        Assert.Same(OutputResolution.FullHd50, resolution);
        Assert.Same(VideoInput.FrontComposite, VideoInput.FromKey("front-composite"));
    }

    [Theory]
    [InlineData("1080P50")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void TryFromKey_rejects_unknown_keys_without_inventing_a_member(string? key)
    {
        Assert.False(OutputResolution.TryFromKey(key, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void FromKey_throws_for_unknown_keys()
    {
        Assert.Throws<ArgumentException>(() => HdrMode.FromKey("hdr11"));
    }

    [Fact]
    public void Equality_is_by_key_and_members_are_stable_singletons()
    {
        Assert.Equal(HdrMode.Off, HdrMode.FromKey("off"));
        Assert.True(HdrMode.Off == HdrMode.FromKey("off"));
        Assert.NotEqual(HdrMode.Off, HdrMode.Hdr10);
        Assert.True(HdrMode.Off != HdrMode.Hdr10);
        Assert.Same(HdrMode.Off, HdrMode.Off);
        Assert.False(HdrMode.Off.Equals(null));
        Assert.Equal("off", HdrMode.Off.ToString());
        Assert.Equal("Off", HdrMode.Off.DisplayName);
    }

    [Fact]
    public void Explicit_Auto_and_Off_values_are_values_in_their_own_right()
    {
        Assert.NotNull(VideoStandard.Auto);
        Assert.NotNull(HdrMode.Off);
        Assert.NotEqual<object>(VideoStandard.Auto, HdrMode.Off);
    }

    [Fact]
    public void Output_resolutions_and_video_modes_carry_their_characteristics()
    {
        Assert.Equal((1920, 1080, 50), (OutputResolution.FullHd50.Width, OutputResolution.FullHd50.Height, OutputResolution.FullHd50.RefreshRateHz));
        Assert.Equal((576, true, 50), (VideoMode.Pal576i.ActiveLines, VideoMode.Pal576i.IsInterlaced, VideoMode.Pal576i.RefreshRateHz));
        Assert.Equal((480, false, 60), (VideoMode.Edtv480p.ActiveLines, VideoMode.Edtv480p.IsInterlaced, VideoMode.Edtv480p.RefreshRateHz));
    }

    [Fact]
    public void Every_standard_smart_enum_has_unique_keys()
    {
        AssertUniqueKeys(VideoInput.All);
        AssertUniqueKeys(VideoStandard.All);
        AssertUniqueKeys(OutputResolution.All);
        AssertUniqueKeys(HdrMode.All);
        AssertUniqueKeys(Colorimetry.All);
        AssertUniqueKeys(RgbRange.All);
        AssertUniqueKeys(DeinterlacingMode.All);
        AssertUniqueKeys(TelecineMode.All);
        AssertUniqueKeys(ScalingMode.All);
        AssertUniqueKeys(VideoMode.All);
    }

    private static void AssertUniqueKeys<T>(IReadOnlyList<T> members)
        where T : SmartEnum<T>, ISmartEnum<T>
    {
        Assert.NotEmpty(members);
        Assert.Equal(members.Count, members.Select(m => m.Key).Distinct(StringComparer.Ordinal).Count());
    }
}
