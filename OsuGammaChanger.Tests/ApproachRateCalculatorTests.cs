using OsuGammaChanger.Core;

namespace OsuGammaChanger.Tests;

public class ApproachRateCalculatorTests
{
    [Theory]
    [InlineData(8, (int)OsuMods.DoubleTime, 9.6667)]
    [InlineData(8, (int)OsuMods.Nightcore, 9.6667)]
    [InlineData(9, (int)OsuMods.DoubleTime, 10.3333)]
    [InlineData(9, (int)OsuMods.Nightcore, 10.3333)]
    [InlineData(10, (int)OsuMods.DoubleTime, 11.0)]
    [InlineData(9, (int)(OsuMods.HardRock | OsuMods.DoubleTime), 11.0)]
    [InlineData(0, (int)(OsuMods.Easy | OsuMods.HalfTime), -5.0)]
    [InlineData(5, (int)(OsuMods.Easy | OsuMods.HalfTime), -1.6667)]
    public void CalculatesKnownEffectiveArCases(double baseAr, int rawMods, double expected)
    {
        var result = ApproachRateCalculator.Calculate(baseAr, rawMods);

        Assert.Equal(expected, result.EffectiveAr, precision: 4);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(-5)]
    [InlineData(11)]
    public void ConvertsArToPreemptAndBack(double ar)
    {
        var preempt = ApproachRateCalculator.PreemptFromAr(ar);
        var converted = ApproachRateCalculator.ArFromPreempt(preempt);

        Assert.Equal(ar, converted, precision: 8);
    }
}
