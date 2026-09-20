namespace OsuGammaChanger.Core;

public static class ConfigValidator
{
    public static ConfigValidationResult Validate(AppConfig config)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (config.PollingIntervalMs < 50)
            errors.Add("pollingIntervalMs must be at least 50.");

        if (!string.IsNullOrWhiteSpace(config.TosuApiUrl))
        {
            if (!Uri.TryCreate(config.TosuApiUrl, UriKind.Absolute, out var tosuUri) ||
                (tosuUri.Scheme != Uri.UriSchemeHttp && tosuUri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add("tosuApiUrl must be an absolute HTTP or HTTPS URL, or empty to disable osu!lazer support.");
            }
            else if (!tosuUri.IsLoopback)
            {
                warnings.Add("tosuApiUrl is not a loopback address; tosu normally runs on this computer.");
            }
        }

        if (config.ArRanges.Count == 0)
            warnings.Add("arRanges is empty; no gameplay gamma will be applied.");

        for (var i = 0; i < config.ArRanges.Count; i++)
        {
            var range = config.ArRanges[i];
            if (!double.IsFinite(range.MinAR))
                errors.Add($"arRanges[{i}].minAR must be finite.");
            if (!double.IsFinite(range.MaxAR))
                errors.Add($"arRanges[{i}].maxAR must be finite.");
            if (!double.IsFinite(range.Gamma))
                errors.Add($"arRanges[{i}].gamma must be finite.");
            if (range.MinAR > range.MaxAR)
                errors.Add($"arRanges[{i}] has minAR greater than maxAR.");
            if (range.Gamma is < 0.2 or > 5.0)
                errors.Add($"arRanges[{i}].gamma must be between 0.2 and 5.0.");
        }

        for (var i = 0; i < config.ArRanges.Count; i++)
        {
            for (var j = i + 1; j < config.ArRanges.Count; j++)
            {
                if (RangesOverlap(config.ArRanges[i], config.ArRanges[j]))
                    warnings.Add($"arRanges[{i}] overlaps arRanges[{j}]; first matching range will be used.");
            }
        }

        return new ConfigValidationResult(errors, warnings);
    }

    private static bool RangesOverlap(GammaRange left, GammaRange right)
        => left.MinAR <= right.MaxAR && right.MinAR <= left.MaxAR;
}
