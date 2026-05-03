namespace OsuGammaChanger.Core;

public sealed record GammaRamp(ushort[] Red, ushort[] Green, ushort[] Blue)
{
    public GammaRamp Copy()
        => new(Red.ToArray(), Green.ToArray(), Blue.ToArray());
}
