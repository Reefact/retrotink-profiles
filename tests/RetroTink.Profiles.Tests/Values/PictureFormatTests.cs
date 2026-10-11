using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Tests.Values;

public class PictureFormatTests
{
    [Fact]
    public void FullFrame_has_no_content_ratio()
    {
        Assert.Equal(PictureFormatKind.FullFrame, PictureFormat.FullFrame.Kind);
        Assert.Null(PictureFormat.FullFrame.ContentAspectRatio);
        Assert.False(PictureFormat.FullFrame.IsLetterboxed);
        Assert.Equal("full-frame", PictureFormat.FullFrame.ToString());
    }

    [Fact]
    public void Letterboxed_formats_are_equal_when_their_ratios_are_equal()
    {
        var a = PictureFormat.Letterboxed(AspectRatio.From(235, 100));
        var b = PictureFormat.Letterboxed(AspectRatio.From(47, 20));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a.IsLetterboxed);
        Assert.Equal("letterboxed(47:20)", a.ToString());
    }

    [Fact]
    public void Different_kinds_or_ratios_are_not_equal()
    {
        Assert.NotEqual(PictureFormat.FullFrame, PictureFormat.Letterboxed(AspectRatio.FourThree));
        Assert.NotEqual(PictureFormat.Letterboxed(AspectRatio.From(235, 100)), PictureFormat.Letterboxed(AspectRatio.From(239, 100)));
    }

    [Fact]
    public void Letterboxed_requires_a_ratio()
    {
        Assert.Throws<ArgumentNullException>(() => PictureFormat.Letterboxed(null!));
    }
}
