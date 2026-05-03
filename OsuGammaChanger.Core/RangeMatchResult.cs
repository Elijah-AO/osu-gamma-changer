namespace OsuGammaChanger.Core;

public sealed record RangeMatchResult(GammaRange? Range, int? RangeIndex, IReadOnlyList<string> Warnings)
{
    public bool HasMatch => Range is not null;
}
