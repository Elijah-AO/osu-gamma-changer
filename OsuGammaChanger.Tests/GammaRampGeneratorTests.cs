using OsuGammaChanger.Core;

namespace OsuGammaChanger.Tests;

public class GammaRampGeneratorTests
{
    [Fact]
    public void GeneratesNeutralRampForAppGammaOne()
    {
        var ramp = GammaRampGenerator.Generate(1.0);

        Assert.Equal((ushort)0, ramp.Red[0]);
        Assert.Equal(ushort.MaxValue, ramp.Red[^1]);
        Assert.Equal((ushort)Math.Round(128 / 255d * ushort.MaxValue), ramp.Red[128]);
    }

    [Fact]
    public void GammaAboveOneBrightensMidpointOnAppScale()
    {
        var neutral = GammaRampGenerator.Generate(1.0);
        var brighter = GammaRampGenerator.Generate(1.45);

        Assert.True(brighter.Red[128] > neutral.Red[128]);
    }
}
