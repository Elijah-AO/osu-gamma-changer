namespace OsuGammaChanger.Tray;

internal static class AppPaths
{
    public static string AppDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "osu-gamma-changer");

    public static string ConfigPath => Path.Combine(AppDirectory, "config.json");
    public static string LogsDirectory => Path.Combine(AppDirectory, "logs");
}
