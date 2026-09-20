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

    [Theory]
    [InlineData(9, 9, 1.2, 9.6667)]
    [InlineData(9, 9, 1.5, 10.3333)]
    [InlineData(8, 10, 0.75, 9)]
    public void CalculatesUsingProviderAdjustedArAndCustomRate(
        double baseAr,
        double adjustedAr,
        double speedMultiplier,
        double expected)
    {
        var result = ApproachRateCalculator.Calculate(
            baseAr,
            (int)OsuMods.DoubleTime,
            adjustedAr,
            speedMultiplier);

        Assert.Equal(expected, result.EffectiveAr, precision: 4);
        Assert.Equal(adjustedAr, result.DifficultyAdjustedAr);
        Assert.Equal(speedMultiplier, result.SpeedMultiplier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void RejectsInvalidCustomRate(double speedMultiplier)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApproachRateCalculator.Calculate(9, 0, 9, speedMultiplier));
    }

    [Theory]
    [InlineData(9, 11, 1.5, 10)]
    [InlineData(9, 9.6667, 1.2, 9)]
    [InlineData(8, 9, 0.75, 10)]
    public void UsesProviderEffectiveArWithoutApplyingRateTwice(
        double baseAr,
        double effectiveAr,
        double speedMultiplier,
        double expectedDifficultyAdjustedAr)
    {
        var result = ApproachRateCalculator.FromEffectiveAr(
            baseAr,
            (int)OsuMods.DoubleTime,
            effectiveAr,
            speedMultiplier);

        Assert.Equal(effectiveAr, result.EffectiveAr, precision: 4);
        Assert.Equal(expectedDifficultyAdjustedAr, result.DifficultyAdjustedAr, precision: 4);
        Assert.Equal(speedMultiplier, result.SpeedMultiplier);
    }
}
