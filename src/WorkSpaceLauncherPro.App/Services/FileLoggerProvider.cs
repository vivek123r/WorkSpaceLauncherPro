using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;

namespace WorkSpaceLauncherPro.App.Services;

/// <summary>
/// Simple file logger that writes one file per day at Information+ level.
/// Format: [Timestamp] [Level] [Category] Message {Exception}
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public FileLoggerProvider(string directory) => _directory = directory;

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, _ => new FileLogger(_directory, categoryName));

    public void Dispose() => _loggers.Clear();
}

internal sealed class FileLogger : ILogger
{
    private readonly string _directory;
    private readonly string _category;
    private readonly object _gate = new();

    public FileLogger(string directory, string category)
    {
        _directory = directory;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel,-11}] [{_category}] {formatter(state, exception)}";
        if (exception is not null) line += Environment.NewLine + exception;
        lock (_gate)
        {
            var fileName = Path.Combine(_directory, $"{DateTime.Now:yyyy-MM-dd}.log");
            File.AppendAllText(fileName, line + Environment.NewLine);
        }
    }
}
