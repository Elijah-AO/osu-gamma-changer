namespace OsuGammaChanger.Core;

[Flags]
public enum OsuMods
{
    None = 0,
    NoFail = 1,
    Easy = 2,
    TouchDevice = 4,
    Hidden = 8,
    HardRock = 16,
    SuddenDeath = 32,
    DoubleTime = 64,
    Relax = 128,
    HalfTime = 256,
    Nightcore = 512,
    Flashlight = 1024,
    Autoplay = 2048,
    SpunOut = 4096,
    Autopilot = 8192,
    Perfect = 16384,
    ScoreV2 = 536870912
}
