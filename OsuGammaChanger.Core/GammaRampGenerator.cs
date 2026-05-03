namespace OsuGammaChanger.Core;

public static class GammaRampGenerator
{
    public const int RampLength = 256;

    public static GammaRamp Generate(double gamma)
    {
        if (!double.IsFinite(gamma) || gamma is <= 0)
            throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be finite and greater than zero.");

        var values = new ushort[RampLength];
        for (var i = 0; i < RampLength; i++)
        {
            var normalized = i / 255d;
            var corrected = Math.Pow(normalized, 1d / gamma);
            values[i] = (ushort)Math.Clamp((int)Math.Round(corrected * ushort.MaxValue), 0, ushort.MaxValue);
        }

        return new GammaRamp(values, values.ToArray(), values.ToArray());
    }
}
