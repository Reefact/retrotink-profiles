using Xunit;

namespace RetroTink.Profiles.Tests;

public class SettingIdTests
{
    [Theory]
    [InlineData("input")]
    [InlineData("output.resolution")]
    [InlineData("geometry.pictureFormat")]
    [InlineData("a.b.c1.d2")]
    public void From_accepts_dotted_identifiers(string text)
    {
        Assert.Equal(text, SettingId.From(text).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1output")]
    [InlineData("output.")]
    [InlineData(".output")]
    [InlineData("output..resolution")]
    [InlineData("output-resolution")]
    [InlineData("output resolution")]
    [InlineData("output.1080p")]
    public void From_rejects_malformed_identifiers(string text)
    {
        Assert.Throws<ArgumentException>(() => SettingId.From(text));
        Assert.False(SettingId.TryFrom(text, out _));
    }

    [Fact]
    public void Equality_is_case_sensitive()
    {
        Assert.Equal(SettingId.From("output.hdr"), SettingId.From("output.hdr"));
        Assert.NotEqual(SettingId.From("output.hdr"), SettingId.From("output.HDR"));
    }

    [Fact]
    public void From_rejects_identifiers_longer_than_the_maximum()
    {
        Assert.False(SettingId.TryFrom("a." + new string('b', SettingId.MaxLength), out _));
    }
}
