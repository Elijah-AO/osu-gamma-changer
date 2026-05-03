namespace OsuGammaChanger.Core;

public static class RangeMatcher
{
    public static RangeMatchResult Match(IReadOnlyList<GammaRange> ranges, double effectiveAr)
    {
        var warnings = new List<string>();
        var matches = ranges
            .Select((range, index) => new IndexedRange(index, range))
            .Where(item => item.Range.MinAR <= effectiveAr && item.Range.MaxAR >= effectiveAr)
            .ToList();

        if (matches.Count > 1)
            warnings.Add($"Effective AR {effectiveAr:0.####} matched {matches.Count} ranges; using first range at index {matches[0].Index}.");

        var selected = matches.Count == 0 ? null : matches[0];
        return selected?.Range is null
            ? new RangeMatchResult(null, null, warnings)
            : new RangeMatchResult(selected.Range, selected.Index, warnings);
    }

    private sealed record IndexedRange(int Index, GammaRange Range);
}
