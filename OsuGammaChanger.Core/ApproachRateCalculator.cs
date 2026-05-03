namespace OsuGammaChanger.Core;

public static class ApproachRateCalculator
{
    public static EffectiveArResult Calculate(double baseAr, int rawMods)
    {
        if (!double.IsFinite(baseAr))
            throw new ArgumentOutOfRangeException(nameof(baseAr), "Base AR must be finite.");

        var mods = (OsuMods)rawMods;
        var difficultyAdjustedAr = baseAr;

        if (mods.HasFlag(OsuMods.Easy))
            difficultyAdjustedAr *= 0.5d;

        if (mods.HasFlag(OsuMods.HardRock))
            difficultyAdjustedAr = Math.Min(difficultyAdjustedAr * 1.4d, 10d);

        var speedMultiplier = GetSpeedMultiplier(mods);
        var preempt = PreemptFromAr(difficultyAdjustedAr);
        var effectivePreempt = preempt / speedMultiplier;
        var effectiveAr = ArFromPreempt(effectivePreempt);

        return new EffectiveArResult(
            baseAr,
            rawMods,
            mods,
            difficultyAdjustedAr,
            speedMultiplier,
            preempt,
            effectivePreempt,
            effectiveAr);
    }

    public static double PreemptFromAr(double ar)
    {
        return ar < 5d
            ? 1200d + 120d * (5d - ar)
            : 1200d - 150d * (ar - 5d);
    }

    public static double ArFromPreempt(double preemptMs)
    {
        if (!double.IsFinite(preemptMs))
            throw new ArgumentOutOfRangeException(nameof(preemptMs), "Preempt must be finite.");

        return preemptMs > 1200d
            ? 5d - (preemptMs - 1200d) / 120d
            : 5d + (1200d - preemptMs) / 150d;
    }

    public static double GetSpeedMultiplier(OsuMods mods)
    {
        if (mods.HasFlag(OsuMods.DoubleTime) || mods.HasFlag(OsuMods.Nightcore))
            return 1.5d;

        if (mods.HasFlag(OsuMods.HalfTime))
            return 0.75d;

        return 1d;
    }
}
