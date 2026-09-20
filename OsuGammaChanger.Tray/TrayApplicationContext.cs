using System.Diagnostics;
using OsuGammaChanger.Core;

namespace OsuGammaChanger.Tray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly FileLogger _logger = new(AppPaths.LogsDirectory);
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly CancellationTokenSource _shutdown = new();
    private AppConfig _config = new();
    private ConfigValidationResult _configValidation = new([], []);
    private OsuStateReader _osuStateReader;
    private Win32GammaDevice? _gammaDevice;
    private GammaController? _gammaController;
    private string _lastPollSignature = string.Empty;
    private string _currentMonitorDeviceName = string.Empty;
    private bool _isExiting;

    public TrayApplicationContext()
    {
        _osuStateReader = new OsuStateReader(_logger);
        _statusItem = new ToolStripMenuItem("Starting...") { Enabled = false };
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "osu! gamma changer",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _logger.Info("osu! gamma changer starting.");
        LoadConfig(showBalloon: true);

        _timer.Tick += async (_, _) => await OnPollTimerTickAsync();
        _timer.Interval = Math.Max(_config.PollingIntervalMs, 50);
        _timer.Start();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Emergency restore gamma", null, (_, _) => EmergencyRestore());
        menu.Items.Add("Reload config", null, (_, _) => LoadConfig(showBalloon: true));
        menu.Items.Add("Open config", null, (_, _) => OpenPath(AppPaths.ConfigPath));
        menu.Items.Add("Open logs", null, (_, _) => OpenPath(AppPaths.LogsDirectory));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void LoadConfig(bool showBalloon)
    {
        try
        {
            _config = AppConfig.LoadOrCreate(AppPaths.ConfigPath);
            _configValidation = ConfigValidator.Validate(_config);
            _timer.Interval = Math.Max(_config.PollingIntervalMs, 50);

            foreach (var warning in _configValidation.Warnings)
                _logger.Warning($"Config warning: {warning}");

            if (!_configValidation.IsValid)
            {
                foreach (var error in _configValidation.Errors)
                    _logger.Error($"Config error: {error}");

                _statusItem.Text = "Config invalid; gamma disabled";
                if (showBalloon)
                    ShowBalloon("Config invalid", "Config has errors. Check logs before playing.");
                return;
            }

            EnsureGammaDevice();
            _statusItem.Text = "Ready";
            _logger.Info($"Config loaded. ranges={_config.ArRanges.Count}, pollingIntervalMs={_config.PollingIntervalMs}, monitor='{_config.MonitorDeviceName}', osuSongsDirectory='{_config.OsuSongsDirectory}', tosuApiUrl='{_config.TosuApiUrl}'");
            if (showBalloon)
                ShowBalloon("Config loaded", "osu! gamma changer is ready.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load config.");
            _statusItem.Text = "Config load failed; gamma disabled";
            if (showBalloon)
                ShowBalloon("Config load failed", "Check logs for details.");
        }
    }

    private async Task OnPollTimerTickAsync()
    {
        _timer.Stop();
        try
        {
            await PollOnceAsync();
        }
        catch (OperationCanceledException) when (_isExiting)
        {
            // Normal during application shutdown.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected polling failure.");
            _statusItem.Text = "Polling failed; check logs";
            _gammaController?.HandleNotPlaying(
                _config.RestoreOriginalRampWhenNotPlaying,
                $"unexpected polling failure: {ex.Message}");
        }
        finally
        {
            if (!_isExiting)
            {
                _timer.Interval = Math.Max(_config.PollingIntervalMs, 50);
                _timer.Start();
            }
        }
    }

    private async Task PollOnceAsync()
    {
        if (!_configValidation.IsValid)
            return;

        EnsureGammaDevice();
        if (_gammaController is null)
        {
            _logger.Warning("No valid gamma device available; polling osu! but gamma changes are disabled.");
            return;
        }

        var state = await _osuStateReader.PollAsync(_config, _shutdown.Token);
        if (state.Signature != _lastPollSignature)
        {
            _lastPollSignature = state.Signature;
            _logger.Info($"osu! state changed: {state.Detail}");
        }

        if (!state.OsuProcessReadable || !state.IsActiveGameplay)
        {
            _statusItem.Text = state.OsuProcessReadable
                ? $"Not playing: {state.Status?.ToString() ?? "unknown"}"
                : state.Client == "lazer"
                    ? "lazer detected; tosu unavailable"
                    : "osu! not detected";
            _gammaController.HandleNotPlaying(_config.RestoreOriginalRampWhenNotPlaying, state.Detail);
            return;
        }

        if (!state.BaseAr.HasValue)
        {
            _statusItem.Text = "Playing: AR unreadable";
            _logger.Warning($"Active gameplay detected but base AR is unavailable. {state.Detail}");
            _gammaController.HandleNoRangeMatch(_config.RestoreOriginalRampWhenNoRangeMatches, "base AR unavailable");
            return;
        }

        var effective = state.EffectiveAr.HasValue && state.SpeedMultiplier.HasValue
            ? ApproachRateCalculator.FromEffectiveAr(
                state.BaseAr.Value,
                state.RawMods,
                state.EffectiveAr.Value,
                state.SpeedMultiplier.Value)
            : ApproachRateCalculator.Calculate(state.BaseAr.Value, state.RawMods);
        _logger.Info($"AR calculation: baseAR={effective.BaseAr:0.####}, baseSource={state.BaseArSource}, rawMods={effective.RawMods}, mods={effective.Mods}, difficultyAdjustedAR={effective.DifficultyAdjustedAr:0.####}, speed={effective.SpeedMultiplier:0.##}, preemptMs={effective.PreemptMs:0.####}, effectivePreemptMs={effective.EffectivePreemptMs:0.####}, effectiveAR={effective.EffectiveAr:0.####}, beatmapId={state.BeatmapId}, hash={state.BeatmapHash}, path={state.BeatmapPath ?? "(unresolved)"}");

        var match = RangeMatcher.Match(_config.ArRanges, effective.EffectiveAr);
        foreach (var warning in match.Warnings)
            _logger.Warning(warning);

        if (!match.HasMatch)
        {
            _statusItem.Text = $"Playing: AR {effective.EffectiveAr:0.##}, no range";
            _logger.Warning($"No AR range matched effectiveAR={effective.EffectiveAr:0.####}; restoreOriginalRampWhenNoRangeMatches={_config.RestoreOriginalRampWhenNoRangeMatches}.");
            _gammaController.HandleNoRangeMatch(_config.RestoreOriginalRampWhenNoRangeMatches, state.Detail);
            return;
        }

        _statusItem.Text = $"Playing: AR {effective.EffectiveAr:0.##}, gamma {match.Range!.Gamma:0.##}";
        _logger.Info($"Matched range index={match.RangeIndex}, minAR={match.Range.MinAR:0.####}, maxAR={match.Range.MaxAR:0.####}, gamma={match.Range.Gamma:0.####}.");
        _gammaController.ApplyMatchedGamma(match.Range.Gamma, state.Detail);
    }

    private void EnsureGammaDevice()
    {
        var requestedDeviceName = string.IsNullOrWhiteSpace(_config.MonitorDeviceName)
            ? Screen.PrimaryScreen?.DeviceName ?? string.Empty
            : _config.MonitorDeviceName;

        if (_gammaController is not null && requestedDeviceName == _currentMonitorDeviceName)
            return;

        _gammaController?.RestoreOriginalIfAvailable("monitor changed", requestedDeviceName);
        _gammaDevice?.Dispose();
        _gammaDevice = null;
        _gammaController = null;
        _currentMonitorDeviceName = requestedDeviceName;

        if (string.IsNullOrWhiteSpace(requestedDeviceName))
        {
            _logger.Warning("No monitor device name available.");
            return;
        }

        var device = new Win32GammaDevice(requestedDeviceName);
        if (!device.IsValid)
        {
            _logger.Warning($"Configured monitor '{requestedDeviceName}' is invalid. Falling back to primary screen.");
            device.Dispose();
            requestedDeviceName = Screen.PrimaryScreen?.DeviceName ?? requestedDeviceName;
            device = new Win32GammaDevice(requestedDeviceName);
        }

        if (!device.IsValid)
        {
            _logger.Error($"Failed to create gamma device for '{requestedDeviceName}'.");
            device.Dispose();
            return;
        }

        _gammaDevice = device;
        _currentMonitorDeviceName = requestedDeviceName;
        _gammaController = new GammaController(device, _logger.Info, _logger.Warning);
        _logger.Info($"Gamma device ready: {requestedDeviceName}. Startup did not change gamma.");
    }

    private void EmergencyRestore()
    {
        _logger.Warning("Emergency restore requested from tray menu.");
        var restored = _gammaController?.RestoreOriginalIfAvailable("emergency restore", "tray menu") ?? false;
        ShowBalloon("Emergency restore", restored ? "Captured original gamma ramp restored." : "No captured original ramp was available.");
    }

    protected override void ExitThreadCore()
    {
        _isExiting = true;
        _logger.Info("Application exit requested.");
        _timer.Stop();
        _shutdown.Cancel();
        if (_config.RestoreOriginalRampOnExit)
            _gammaController?.RestoreOriginalIfAvailable("application exit", "ExitThreadCore");
        else
            _logger.Warning("restoreOriginalRampOnExit=false; leaving current gamma state unchanged on exit.");

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _osuStateReader.Dispose();
        _shutdown.Dispose();
        _gammaDevice?.Dispose();
        _logger.Info("osu! gamma changer stopped.");
        _logger.Dispose();
        base.ExitThreadCore();
    }

    private void ShowBalloon(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(3000);
    }

    private void OpenPath(string path)
    {
        try
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to open path: {path}");
        }
    }
}
