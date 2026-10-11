using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Tests.Values;

public class AspectRatioTests
{
    [Fact]
    public void From_reduces_to_a_canonical_fraction()
    {
        var ratio = AspectRatio.From(235, 100);

        Assert.Equal(47, ratio.Numerator);
        Assert.Equal(20, ratio.Denominator);
        Assert.Equal("47:20", ratio.ToString());
        Assert.Equal(2.35, ratio.Value, precision: 10);
    }

    [Fact]
    public void Equal_fractions_are_equal_whatever_their_original_terms()
    {
        Assert.Equal(AspectRatio.From(235, 100), AspectRatio.From(47, 20));
        Assert.Equal(AspectRatio.From(8, 6), AspectRatio.FourThree);
        Assert.Equal(AspectRatio.From(235, 100).GetHashCode(), AspectRatio.From(47, 20).GetHashCode());
        Assert.True(AspectRatio.From(32, 18) == AspectRatio.SixteenNine);
    }

    [Fact]
    public void Close_ratios_are_not_confused()
    {
        Assert.NotEqual(AspectRatio.From(235, 100), AspectRatio.From(239, 100));
        Assert.NotEqual(AspectRatio.From(2, 1), AspectRatio.From(1, 2));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-4, 3)]
    [InlineData(4, -3)]
    public void From_rejects_non_positive_terms(int numerator, int denominator)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AspectRatio.From(numerator, denominator));
        Assert.False(AspectRatio.TryFrom(numerator, denominator, out _));
    }

    [Fact]
    public void IsWiderThan_compares_exactly()
    {
        Assert.True(AspectRatio.From(235, 100).IsWiderThan(AspectRatio.SixteenNine));
        Assert.True(AspectRatio.SixteenNine.IsWiderThan(AspectRatio.FourThree));
        Assert.False(AspectRatio.FourThree.IsWiderThan(AspectRatio.From(8, 6)));
    }
}
