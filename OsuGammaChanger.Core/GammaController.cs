namespace OsuGammaChanger.Core;

public sealed class GammaController
{
    private readonly IGammaDevice _device;
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logWarning;
    private GammaRamp? _originalRamp;
    private double? _lastAppliedGamma;

    public GammaController(IGammaDevice device, Action<string> logInfo, Action<string> logWarning)
    {
        _device = device;
        _logInfo = logInfo;
        _logWarning = logWarning;
    }

    public bool HasOriginalRamp => _originalRamp is not null;
    public bool HasChangedGamma => _lastAppliedGamma.HasValue;
    public double? LastAppliedGamma => _lastAppliedGamma;

    public bool ApplyMatchedGamma(double gamma, string context)
    {
        if (_lastAppliedGamma.HasValue && Math.Abs(_lastAppliedGamma.Value - gamma) < 0.0001)
        {
            _logInfo($"Gamma {gamma:0.####} already applied; skipping repeated write. Context: {context}");
            return true;
        }

        if (_originalRamp is null)
        {
            if (!_device.TryGetCurrentRamp(out var captured, out var captureError))
            {
                _logWarning($"Failed to capture original gamma ramp before applying gamma {gamma:0.####}. Context: {context}. Error: {captureError}");
                return false;
            }

            _originalRamp = captured.Copy();
            _logInfo($"Captured original gamma ramp for {_device.DeviceName} before first generated gamma write.");
        }

        var generated = GammaRampGenerator.Generate(gamma);
        if (!_device.TrySetRamp(generated, out var applyError))
        {
            _logWarning($"Failed to apply generated gamma {gamma:0.####}. Context: {context}. Error: {applyError}");
            return false;
        }

        _lastAppliedGamma = gamma;
        _logInfo($"Applied generated gamma {gamma:0.####}. Context: {context}");
        return true;
    }

    public bool HandleNotPlaying(bool restoreOriginalRampWhenNotPlaying, string context)
    {
        if (!HasChangedGamma)
            return true;

        if (!restoreOriginalRampWhenNotPlaying)
        {
            _logInfo($"Not playing and restoreOriginalRampWhenNotPlaying=false; leaving current gamma state unchanged. Context: {context}");
            return true;
        }

        return RestoreOriginalIfAvailable("not playing", context);
    }

    public bool HandleNoRangeMatch(bool restoreOriginalRampWhenNoRangeMatches, string context)
    {
        if (!HasChangedGamma)
        {
            _logInfo($"No AR range matched and app has not changed gamma; doing nothing. Context: {context}");
            return true;
        }

        if (!restoreOriginalRampWhenNoRangeMatches)
        {
            _logWarning($"No AR range matched and restoreOriginalRampWhenNoRangeMatches=false; leaving last app gamma in place. Context: {context}");
            return true;
        }

        return RestoreOriginalIfAvailable("no AR range matched", context);
    }

    public bool RestoreOriginalIfAvailable(string reason, string context)
    {
        if (_originalRamp is null)
        {
            _logInfo($"No captured original gamma ramp available; restore skipped. Reason: {reason}. Context: {context}");
            return false;
        }

        if (!_device.TrySetRamp(_originalRamp.Copy(), out var restoreError))
        {
            _logWarning($"Failed to restore captured original gamma ramp. Reason: {reason}. Context: {context}. Error: {restoreError}");
            return false;
        }

        _lastAppliedGamma = null;
        _logInfo($"Restored captured original gamma ramp. Reason: {reason}. Context: {context}");
        return true;
    }
}
