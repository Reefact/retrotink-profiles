using Xunit;

namespace RetroTink.Profiles.Tests;

public class ProfileIdTests
{
    [Theory]
    [InlineData("ld-pal")]
    [InlineData("a")]
    [InlineData("epson.fullhd_v2")]
    [InlineData("0123")]
    public void From_accepts_canonical_identifiers(string text)
    {
        var id = ProfileId.From(text);

        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Fact]
    public void From_normalises_case_so_that_identifiers_are_case_insensitive()
    {
        var upper = ProfileId.From("LD-PAL");
        var lower = ProfileId.From("ld-pal");

        Assert.Equal("ld-pal", upper.Value);
        Assert.Equal(lower, upper);
        Assert.Equal(lower.GetHashCode(), upper.GetHashCode());
        Assert.True(lower == upper);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ld pal")]
    [InlineData("-ld-pal")]
    [InlineData("ld-pal-")]
    [InlineData(".ld")]
    [InlineData("ld/pal")]
    [InlineData("lasérdisc")]
    [InlineData("ld\tpal")]
    public void From_rejects_invalid_identifiers(string text)
    {
        Assert.Throws<ArgumentException>(() => ProfileId.From(text));
        Assert.False(ProfileId.TryFrom(text, out _));
    }

    [Fact]
    public void From_rejects_identifiers_longer_than_the_maximum()
    {
        Assert.False(ProfileId.TryFrom(new string('a', ProfileId.MaxLength + 1), out _));
        Assert.True(ProfileId.TryFrom(new string('a', ProfileId.MaxLength), out _));
    }

    [Fact]
    public void From_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => ProfileId.From(null!));
        Assert.False(ProfileId.TryFrom(null, out _));
    }

    [Fact]
    public void Different_identifiers_are_not_equal()
    {
        Assert.NotEqual(ProfileId.From("ld-pal"), ProfileId.From("ld-ntsc"));
        Assert.True(ProfileId.From("ld-pal") != ProfileId.From("ld-ntsc"));
        Assert.False(ProfileId.From("ld-pal").Equals(null));
    }
}
