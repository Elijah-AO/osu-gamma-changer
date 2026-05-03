namespace OsuGammaChanger.Tray;

internal sealed class FileLogger : IDisposable
{
    private readonly object _lock = new();
    private readonly string _logFilePath;
    private StreamWriter _writer;

    public FileLogger(string logsDirectory)
    {
        Directory.CreateDirectory(logsDirectory);
        _logFilePath = Path.Combine(logsDirectory, $"{DateTime.Now:yyyyMMdd}.log");
        _writer = new StreamWriter(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };
    }

    public string LogFilePath => _logFilePath;

    public void Info(string message) => Write("INFO", message);
    public void Warning(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);
    public void Error(Exception exception, string message) => Write("ERROR", $"{message}{Environment.NewLine}{exception}");

    private void Write(string level, string message)
    {
        lock (_lock)
        {
            _writer.WriteLine($"{DateTime.Now:O} [{level}] {message}");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
        }
    }
}
