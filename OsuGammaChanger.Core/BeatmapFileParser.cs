using System.Globalization;

namespace OsuGammaChanger.Core;

public static class BeatmapFileParser
{
    public static double? TryReadApproachRate(string osuFilePath)
    {
        if (string.IsNullOrWhiteSpace(osuFilePath) || !File.Exists(osuFilePath))
            return null;

        var inDifficulty = false;
        foreach (var rawLine in File.ReadLines(osuFilePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
            {
                inDifficulty = line.Equals("[Difficulty]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inDifficulty)
                continue;

            var separator = line.IndexOf(':');
            if (separator < 0)
                continue;

            var key = line[..separator].Trim();
            if (!key.Equals("ApproachRate", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = line[(separator + 1)..].Trim();
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ar)
                ? ar
                : null;
        }

        return null;
    }
}
