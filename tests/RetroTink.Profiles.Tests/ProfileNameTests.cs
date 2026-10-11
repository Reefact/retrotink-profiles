using Xunit;

namespace RetroTink.Profiles.Tests;

public class ProfileNameTests
{
    [Fact]
    public void From_trims_surrounding_white_space_and_keeps_unicode()
    {
        var name = ProfileName.From("  LaserDisc PAL — Cinémascope  ");

        Assert.Equal("LaserDisc PAL — Cinémascope", name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Line\nbreak")]
    [InlineData("Tab\there")]
    public void From_rejects_blank_names_and_control_characters(string text)
    {
        Assert.Throws<ArgumentException>(() => ProfileName.From(text));
        Assert.False(ProfileName.TryFrom(text, out _));
    }

    [Fact]
    public void From_rejects_names_longer_than_the_maximum()
    {
        Assert.False(ProfileName.TryFrom(new string('x', ProfileName.MaxLength + 1), out _));
        Assert.True(ProfileName.TryFrom(new string('x', ProfileName.MaxLength), out _));
    }

    [Fact]
    public void Equality_is_ordinal_and_case_sensitive()
    {
        Assert.Equal(ProfileName.From("PAL"), ProfileName.From(" PAL "));
        Assert.NotEqual(ProfileName.From("PAL"), ProfileName.From("pal"));
    }
}
