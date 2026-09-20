# osu! AR Gamma Changer

Small Windows tray utility that applies this app's generated gamma ramp while an osu!stable or osu!lazer map is actively being played, based on effective AR after mods.

## osu!lazer support

osu!lazer does not currently expose the live beatmap and mod data this app needs through its official external-integration API. Lazer support therefore uses [tosu](https://tosu.app/), which reads that data and exposes it on your computer:

1. Download and run tosu.
2. Leave its API at the default `http://127.0.0.1:24050`.
3. Run osu! gamma changer normally.

Stable support remains built in and does not require tosu. The `tosuApiUrl` config value can be changed if your tosu port is different, or set to an empty string to disable lazer support. Lazer's converted AR already includes adjustable difficulty and custom DT/HT clock rates, and is used directly.

## Configuration

On first launch the app creates:

`%APPDATA%\osu-gamma-changer\config.json`

`gamma` values are this app's own scale. `1.0` means neutral only for generated ramps from this app. Restore behavior always restores the captured original monitor ramp; it never writes a generated `1.0` ramp as a default.
