using OsuGammaChanger.Core;

namespace OsuGammaChanger.Tests;

public class BeatmapFileParserTests
{
    [Fact]
    public void ReadsApproachRateFromDifficultySection()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, """
                osu file format v14

                [General]
                Mode:0

                [Difficulty]
                HPDrainRate:5
                ApproachRate:9.3

                [Events]
                """);

            Assert.Equal(9.3, BeatmapFileParser.TryReadApproachRate(file));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
