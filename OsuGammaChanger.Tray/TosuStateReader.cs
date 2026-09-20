using System.Diagnostics;
using System.Text.Json;
using OsuGammaChanger.Core;
using OsuMemoryDataProvider;

namespace OsuGammaChanger.Tray;

internal sealed class TosuStateReader : IDisposable
{
    private readonly HttpClient _httpClient;
    private OsuPollResult? _preparedState;

    public TosuStateReader()
    {
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectTimeout = TimeSpan.FromMilliseconds(300)
        })
        {
            Timeout = TimeSpan.FromMilliseconds(500)
        };
    }

    public static bool IsLazerRunning()
    {
        foreach (var process in Process.GetProcessesByName("osu!"))
        {
            using (process)
            {
                try
                {
                    var executable = process.MainModule?.FileName;
                    if (executable?.Contains(
                            $"{Path.DirectorySeparatorChar}osulazer{Path.DirectorySeparatorChar}",
                            StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return true;
                    }
                }
                catch
                {
                    // A process owned by another user may not expose its executable path.
                }
            }
        }

        return false;
    }

    public async Task<OsuPollResult?> PollAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.TosuApiUrl))
            return Unavailable("osu!lazer is running, but lazer support is disabled because tosuApiUrl is empty");

        try
        {
            using var response = await _httpClient.GetAsync(
                config.TosuApiUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Unavailable($"osu!lazer is running, but tosu returned HTTP {(int)response.StatusCode}");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var parsed = Parse(document.RootElement);
            return parsed is null ? null : StabilizeGameplayTransition(parsed);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable("osu!lazer is running, but the tosu API timed out");
        }
        catch (HttpRequestException ex)
        {
            return Unavailable($"osu!lazer is running, but tosu is unavailable ({ex.Message})");
        }
        catch (JsonException ex)
        {
            return Unavailable($"osu!lazer is running, but tosu returned invalid JSON ({ex.Message})");
        }
    }

    private static OsuPollResult? Parse(JsonElement root)
    {
        var client = GetString(root, "client");
        if (string.IsNullOrWhiteSpace(client))
            return Unavailable("osu!lazer is running, but the tosu response did not identify an osu! client");

        if (!client.Equals("lazer", StringComparison.OrdinalIgnoreCase))
            return null;

        var state = GetObject(root, "state");
        var stateNumber = GetInt32(state, "number");
        var stateName = GetString(state, "name");
        var isPlaying = stateNumber == 2 || stateName.Equals("play", StringComparison.OrdinalIgnoreCase);

        var beatmap = GetObject(root, "beatmap");
        var stats = GetObject(beatmap, "stats");
        var ar = GetObject(stats, "ar");
        var play = GetObject(root, "play");
        var mods = GetObject(play, "mods");
        var directPath = GetObject(root, "directPath");

        var baseAr = GetFiniteDouble(ar, "original");
        var effectiveAr = GetFiniteDouble(ar, "converted");
        var speedMultiplier = GetFiniteDouble(mods, "rate");
        if (speedMultiplier is <= 0)
            speedMultiplier = null;

        var rawMods = GetInt32(mods, "number") ?? 0;
        var modsName = GetString(mods, "name");
        var beatmapId = GetInt32(beatmap, "id");
        var beatmapSetId = GetInt32(beatmap, "set");
        var beatmapHash = GetString(beatmap, "checksum");
        var beatmapPath = GetString(directPath, "beatmapFile");
        var mode = GetObject(play, "mode");
        var gameMode = GetInt32(mode, "number");

        var detail =
            $"client=lazer, provider=tosu, status={stateName}, rawStatus={stateNumber?.ToString() ?? "(unknown)"}, " +
            $"gameMode={gameMode?.ToString() ?? "(unknown)"}, beatmapId={beatmapId?.ToString() ?? "(unknown)"}, " +
            $"setId={beatmapSetId?.ToString() ?? "(unknown)"}, hash={EmptyAsUnknown(beatmapHash)}, " +
            $"path={EmptyAsUnknown(beatmapPath)}, baseAr={Format(baseAr)}, effectiveAr={Format(effectiveAr)}, " +
            $"speed={Format(speedMultiplier)}, rawMods={rawMods}, mods={EmptyAsUnknown(modsName)}";

        return new OsuPollResult
        {
            Client = "lazer",
            DataProvider = "tosu",
            OsuProcessReadable = true,
            Status = stateNumber.HasValue ? (OsuMemoryStatus)stateNumber.Value : null,
            RawStatus = stateNumber,
            GameMode = gameMode,
            IsActiveGameplay = isPlaying,
            RawMods = rawMods,
            BeatmapId = beatmapId,
            BeatmapSetId = beatmapSetId,
            BeatmapHash = NullIfEmpty(beatmapHash),
            BeatmapPath = NullIfEmpty(beatmapPath),
            BaseAr = baseAr,
            BaseArSource = baseAr.HasValue ? "tosu" : "none",
            EffectiveAr = effectiveAr,
            SpeedMultiplier = speedMultiplier,
            ModsName = modsName,
            Detail = detail
        };
    }

    private OsuPollResult StabilizeGameplayTransition(OsuPollResult current)
    {
        if (!current.IsActiveGameplay)
        {
            if (current.RawStatus == 5 && current.BaseAr.HasValue && current.EffectiveAr.HasValue)
                _preparedState = current;

            return current;
        }

        if (_preparedState is null ||
            !SameBeatmap(_preparedState, current) ||
            !LooksLikeTransientEmptyMods(current, _preparedState))
        {
            return current;
        }

        return current with
        {
            RawMods = _preparedState.RawMods,
            EffectiveAr = _preparedState.EffectiveAr,
            SpeedMultiplier = _preparedState.SpeedMultiplier,
            ModsName = _preparedState.ModsName,
            Detail = current.Detail +
                     $"; using pre-play mod snapshot while lazer initializes " +
                     $"(effectiveAr={Format(_preparedState.EffectiveAr)}, speed={Format(_preparedState.SpeedMultiplier)}, " +
                     $"rawMods={_preparedState.RawMods}, mods={EmptyAsUnknown(_preparedState.ModsName)})"
        };
    }

    private static bool SameBeatmap(OsuPollResult left, OsuPollResult right)
    {
        if (!string.IsNullOrWhiteSpace(left.BeatmapHash) && !string.IsNullOrWhiteSpace(right.BeatmapHash))
            return left.BeatmapHash.Equals(right.BeatmapHash, StringComparison.OrdinalIgnoreCase);

        return left.BeatmapId.HasValue && left.BeatmapId == right.BeatmapId;
    }

    private static bool LooksLikeTransientEmptyMods(OsuPollResult current, OsuPollResult prepared)
    {
        var currentHasNoMods = current.RawMods == 0 && string.IsNullOrWhiteSpace(current.ModsName);
        var preparedHasMods = prepared.RawMods != 0 || !string.IsNullOrWhiteSpace(prepared.ModsName);
        return currentHasNoMods && preparedHasMods;
    }

    private static OsuPollResult Unavailable(string detail) => new()
    {
        Client = "lazer",
        DataProvider = "tosu",
        OsuProcessReadable = false,
        Status = OsuMemoryStatus.NotRunning,
        Detail = detail
    };

    private static JsonElement GetObject(JsonElement parent, string name)
        => parent.ValueKind == JsonValueKind.Object &&
           parent.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.Object
            ? value
            : default;

    private static string GetString(JsonElement parent, string name)
        => parent.ValueKind == JsonValueKind.Object &&
           parent.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static int? GetInt32(JsonElement parent, string name)
        => parent.ValueKind == JsonValueKind.Object &&
           parent.TryGetProperty(name, out var value) &&
           value.TryGetInt32(out var number)
            ? number
            : null;

    private static double? GetFiniteDouble(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(name, out var value) ||
            !value.TryGetDouble(out var number) ||
            !double.IsFinite(number))
        {
            return null;
        }

        return number;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string EmptyAsUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "(unknown)" : value;
    private static string Format(double? value) => value?.ToString("0.####") ?? "(unknown)";

    public void Dispose() => _httpClient.Dispose();
}
