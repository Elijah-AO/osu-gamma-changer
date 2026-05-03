namespace OsuGammaChanger.Core;

public sealed record GammaRange
{
    public double MinAR { get; init; }
    public double MaxAR { get; init; }
    public double Gamma { get; init; }
}
