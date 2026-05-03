namespace OsuGammaChanger.Core;

public interface IGammaDevice
{
    string DeviceName { get; }
    bool TryGetCurrentRamp(out GammaRamp ramp, out string? error);
    bool TrySetRamp(GammaRamp ramp, out string? error);
}
