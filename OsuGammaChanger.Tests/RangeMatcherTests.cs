using OsuGammaChanger.Core;

namespace OsuGammaChanger.Tests;

public class RangeMatcherTests
{
    [Fact]
    public void MatchesInclusiveBounds()
    {
        var ranges = new[]
        {
            new GammaRange { MinAR = 9, MaxAR = 9.5, Gamma = 1.15 }
        };

        Assert.True(RangeMatcher.Match(ranges, 9).HasMatch);
        Assert.True(RangeMatcher.Match(ranges, 9.5).HasMatch);
    }

    [Fact]
    public void UsesFirstMatchingRangeAndWarnsWhenMultipleMatch()
    {
        var ranges = new[]
        {
            new GammaRange { MinAR = 9, MaxAR = 10, Gamma = 1.15 },
            new GammaRange { MinAR = 9.5, MaxAR = 10.5, Gamma = 1.3 }
        };

        var result = RangeMatcher.Match(ranges, 9.75);

        Assert.Equal(0, result.RangeIndex);
        Assert.Equal(1.15, result.Range!.Gamma);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void ReturnsNoMatchWhenArIsOutsideRanges()
    {
        var ranges = new[]
        {
            new GammaRange { MinAR = 9, MaxAR = 10, Gamma = 1.15 }
        };

        var result = RangeMatcher.Match(ranges, 8.99);

        Assert.False(result.HasMatch);
        Assert.Null(result.Range);
    }
}
