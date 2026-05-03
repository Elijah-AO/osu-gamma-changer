using OsuGammaChanger.Core;
using OsuMemoryDataProvider;

namespace OsuGammaChanger.Tray;

internal sealed record OsuPollResult
{
    public bool OsuProcessReadable { get; init; }
    public OsuMemoryStatus? Status { get; init; }
    public int? RawStatus { get; init; }
    public int? GameMode { get; init; }
    public bool IsActiveGameplay { get; init; }
    public bool IsReplay { get; init; }
    public int RawMods { get; init; }
    public OsuMods ParsedMods => (OsuMods)RawMods;
    public int? BeatmapId { get; init; }
    public int? BeatmapSetId { get; init; }
    public string? BeatmapHash { get; init; }
    public string? BeatmapString { get; init; }
    public string? BeatmapPath { get; init; }
    public double? BaseAr { get; init; }
    public string BaseArSource { get; init; } = "none";
    public string Detail { get; init; } = string.Empty;

    public string Signature => string.Join("|",
        OsuProcessReadable,
        Status?.ToString() ?? "null",
        RawStatus?.ToString() ?? "null",
        GameMode?.ToString() ?? "null",
        IsActiveGameplay,
        IsReplay,
        RawMods,
        BeatmapId?.ToString() ?? "null",
        BeatmapHash ?? "null",
        BaseAr?.ToString("0.####") ?? "null",
        BaseArSource,
        Detail);
}
