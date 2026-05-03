# osu! AR Gamma Changer

Small Windows tray utility that applies this app's generated gamma ramp while an osu!stable map is actively being played, based on effective AR after mods.

## Configuration

On first launch the app creates:

`%APPDATA%\osu-gamma-changer\config.json`

`gamma` values are this app's own scale. `1.0` means neutral only for generated ramps from this app. Restore behavior always restores the captured original monitor ramp; it never writes a generated `1.0` ramp as a default.