namespace OsuGammaChanger.Core;

public sealed record EffectiveArResult(
    double BaseAr,
    int RawMods,
    OsuMods Mods,
    double DifficultyAdjustedAr,
    double SpeedMultiplier,
    double PreemptMs,
    double EffectivePreemptMs,
    double EffectiveAr);
