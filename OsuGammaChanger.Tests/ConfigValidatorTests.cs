using OsuGammaChanger.Core;

namespace OsuGammaChanger.Tests;

public class ConfigValidatorTests
{
    [Fact]
    public void RejectsInvalidConfigWithoutThrowing()
    {
        var config = new AppConfig
        {
            PollingIntervalMs = 10,
            ArRanges =
            [
                new GammaRange { MinAR = 10, MaxAR = 9, Gamma = 1 },
                new GammaRange { MinAR = 9, MaxAR = 10, Gamma = 9 }
            ]
        };

        var result = ConfigValidator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("pollingIntervalMs"));
        Assert.Contains(result.Errors, e => e.Contains("minAR greater"));
        Assert.Contains(result.Errors, e => e.Contains("gamma"));
    }

    [Fact]
    public void WarnsWhenRangesOverlap()
    {
        var config = new AppConfig
        {
            ArRanges =
            [
                new GammaRange { MinAR = 9, MaxAR = 10, Gamma = 1.1 },
                new GammaRange { MinAR = 10, MaxAR = 11, Gamma = 1.2 }
            ]
        };

        var result = ConfigValidator.Validate(config);

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void RejectsInvalidTosuApiUrl()
    {
        var config = new AppConfig { TosuApiUrl = "not a URL" };

        var result = ConfigValidator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("tosuApiUrl"));
    }
}
