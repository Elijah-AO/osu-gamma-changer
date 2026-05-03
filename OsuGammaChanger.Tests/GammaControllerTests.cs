using OsuGammaChanger.Core;

namespace OsuGammaChanger.Tests;

public class GammaControllerTests
{
    [Fact]
    public void CapturesOriginalRampBeforeFirstGeneratedWrite()
    {
        var device = new FakeGammaDevice();
        var controller = CreateController(device);

        Assert.True(controller.ApplyMatchedGamma(1.3, "test"));

        Assert.Equal(1, device.GetCurrentRampCalls);
        Assert.Equal(1, device.SetRampCalls);
        Assert.True(controller.HasOriginalRamp);
        Assert.True(controller.HasChangedGamma);
    }

    [Fact]
    public void NoMatchDoesNothingWhenAppNeverChangedGamma()
    {
        var device = new FakeGammaDevice();
        var controller = CreateController(device);

        Assert.True(controller.HandleNoRangeMatch(restoreOriginalRampWhenNoRangeMatches: true, "test"));

        Assert.Equal(0, device.GetCurrentRampCalls);
        Assert.Equal(0, device.SetRampCalls);
    }

    [Fact]
    public void NoMatchRestoresOriginalAfterAppChangedGamma()
    {
        var device = new FakeGammaDevice();
        var controller = CreateController(device);

        controller.ApplyMatchedGamma(1.3, "test");
        Assert.True(controller.HandleNoRangeMatch(restoreOriginalRampWhenNoRangeMatches: true, "test"));

        Assert.Equal(1, device.GetCurrentRampCalls);
        Assert.Equal(2, device.SetRampCalls);
        Assert.False(controller.HasChangedGamma);
    }

    [Fact]
    public void NotPlayingRestoresOriginalWhenConfigured()
    {
        var device = new FakeGammaDevice();
        var controller = CreateController(device);

        controller.ApplyMatchedGamma(1.3, "test");
        Assert.True(controller.HandleNotPlaying(restoreOriginalRampWhenNotPlaying: true, "test"));

        Assert.Equal(2, device.SetRampCalls);
        Assert.False(controller.HasChangedGamma);
    }

    [Fact]
    public void SkipsRepeatedGammaWrite()
    {
        var device = new FakeGammaDevice();
        var controller = CreateController(device);

        controller.ApplyMatchedGamma(1.3, "test");
        controller.ApplyMatchedGamma(1.3, "test");

        Assert.Equal(1, device.GetCurrentRampCalls);
        Assert.Equal(1, device.SetRampCalls);
    }

    private static GammaController CreateController(FakeGammaDevice device)
        => new(device, _ => { }, _ => { });

    private sealed class FakeGammaDevice : IGammaDevice
    {
        private GammaRamp _current = GammaRampGenerator.Generate(1.0);
        public string DeviceName => "Fake";
        public int GetCurrentRampCalls { get; private set; }
        public int SetRampCalls { get; private set; }

        public bool TryGetCurrentRamp(out GammaRamp ramp, out string? error)
        {
            GetCurrentRampCalls++;
            ramp = _current.Copy();
            error = null;
            return true;
        }

        public bool TrySetRamp(GammaRamp ramp, out string? error)
        {
            SetRampCalls++;
            _current = ramp.Copy();
            error = null;
            return true;
        }
    }
}
