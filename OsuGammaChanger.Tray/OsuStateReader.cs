using System.Diagnostics;
using OsuGammaChanger.Core;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;

namespace OsuGammaChanger.Tray;

internal sealed class OsuStateReader
{
    private readonly FileLogger _logger;
    private readonly StructuredOsuMemoryReader _reader = StructuredOsuMemoryReader.Instance;

    public OsuStateReader(FileLogger logger)
    {
        _logger = logger;
    }

    public OsuPollResult Poll(AppConfig config)
    {
        try
        {
            if (!_reader.CanRead)
            {
                return new OsuPollResult
                {
                    OsuProcessReadable = false,
                    Status = OsuMemoryStatus.NotRunning,
                    Detail = "osu! process is not readable or not running"
                };
            }

            var memory = _reader.OsuMemoryAddresses;
            if (!_reader.TryRead(memory.GeneralData))
            {
                return new OsuPollResult
                {
                    OsuProcessReadable = true,
                    Detail = "failed to read osu! general data"
                };
            }

            var general = memory.GeneralData;
            var status = general.OsuStatus;
            var result = new OsuPollResult
            {
                OsuProcessReadable = true,
                Status = status,
                RawStatus = general.RawStatus,
                GameMode = general.GameMode,
                RawMods = general.Mods,
                Detail = $"status={status}, rawStatus={general.RawStatus}, gameMode={general.GameMode}, generalMods={general.Mods}"
            };

            if (status != OsuMemoryStatus.Playing)
                return result with { IsActiveGameplay = false };

            if (!_reader.TryRead(memory.Beatmap))
                return result with { Detail = result.Detail + "; failed to read current beatmap while playing" };

            if (!_reader.TryRead(memory.Player))
                return result with { Detail = result.Detail + "; failed to read player data while playing" };

            var player = memory.Player;
            if (player.IsReplay)
            {
                return CreateGameplayResult(config, memory.Beatmap, result, isActiveGameplay: false, isReplay: true, general.Mods,
                    "playing status is a replay; treating as not active gameplay");
            }

            var gameplayMods = player.Mods?.Value ?? general.Mods;
            return CreateGameplayResult(config, memory.Beatmap, result, isActiveGameplay: true, isReplay: false, gameplayMods,
                "active gameplay detected");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while polling osu! memory.");
            return new OsuPollResult { Detail = $"exception while polling osu!: {ex.Message}" };
        }
    }

    private OsuPollResult CreateGameplayResult(
        AppConfig config,
        CurrentBeatmap beatmap,
        OsuPollResult baseResult,
        bool isActiveGameplay,
        bool isReplay,
        int rawMods,
        string detail)
    {
        var path = ResolveBeatmapPath(config, beatmap);
        var memoryAr = double.IsFinite(beatmap.Ar) && beatmap.Ar is >= -20 and <= 20 ? beatmap.Ar : (double?)null;
        var fileAr = memoryAr.HasValue ? null : BeatmapFileParser.TryReadApproachRate(path ?? string.Empty);
        var baseAr = memoryAr ?? fileAr;
        var arSource = memoryAr.HasValue ? "memory" : fileAr.HasValue ? ".osu file" : "none";

        return baseResult with
        {
            IsActiveGameplay = isActiveGameplay,
            IsReplay = isReplay,
            RawMods = rawMods,
            BeatmapId = beatmap.Id,
            BeatmapSetId = beatmap.SetId,
            BeatmapHash = beatmap.Md5,
            BeatmapString = beatmap.MapString,
            BeatmapPath = path,
            BaseAr = baseAr,
            BaseArSource = arSource,
            Detail = $"{detail}; beatmapId={beatmap.Id}, setId={beatmap.SetId}, hash={beatmap.Md5}, path={path ?? "(unresolved)"}, baseAr={baseAr?.ToString("0.####") ?? "(unknown)"}, baseArSource={arSource}, rawMods={rawMods}, mods={(OsuMods)rawMods}"
        };
    }

    private static string? ResolveBeatmapPath(AppConfig config, CurrentBeatmap beatmap)
    {
        var songsRoot = !string.IsNullOrWhiteSpace(config.OsuSongsDirectory)
            ? config.OsuSongsDirectory
            : TryFindDefaultSongsDirectory();

        if (string.IsNullOrWhiteSpace(songsRoot) ||
            string.IsNullOrWhiteSpace(beatmap.FolderName) ||
            string.IsNullOrWhiteSpace(beatmap.OsuFileName))
        {
            return null;
        }

        try
        {
            return Path.Combine(songsRoot, beatmap.FolderName.TrimEnd(), beatmap.OsuFileName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? TryFindDefaultSongsDirectory()
    {
        try
        {
            var process = Process.GetProcessesByName("osu!").FirstOrDefault();
            var osuDirectory = process?.MainModule?.FileName is { } fileName
                ? Path.GetDirectoryName(fileName)
                : null;

            if (osuDirectory is null)
                return null;

            var songs = Path.Combine(osuDirectory, "Songs");
            return Directory.Exists(songs) ? songs : null;
        }
        catch
        {
            return null;
        }
    }
}
