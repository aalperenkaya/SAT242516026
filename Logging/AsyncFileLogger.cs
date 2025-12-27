using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;

namespace SAT242516026.Logging;

public sealed class AsyncFileLogger : ILogger
{
    private readonly string _filePath;
    private readonly string _categoryName;

    public AsyncFileLogger(string filePath, string categoryName)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _categoryName = categoryName ?? "Unknown";
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public async void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        try
        {
            // klasör yoksa oluştur
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var msgBody = SafeFormat(state, exception, formatter);

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {_categoryName}: {msgBody}";
            if (exception is not null) line += $" | EX: {exception}";
            line += Environment.NewLine;

            await File.AppendAllTextAsync(_filePath, line).ConfigureAwait(false);
        }
        catch
        {
            // log yazarken patlarsa sistemi çökertme
        }
    }

    private static string SafeFormat<TState>(
        TState state,
        Exception? ex,
        Func<TState, Exception?, string>? formatter)
    {
        try
        {
            if (formatter is not null)
                return formatter(state, ex) ?? "";
        }
        catch
        {
            // formatter bozuktur, yut
        }

        // state null gelebilir -> patlama yok
        return state?.ToString() ?? "";
    }
}

public sealed class AsyncFileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;

    public AsyncFileLoggerProvider(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public ILogger CreateLogger(string categoryName)
        => new AsyncFileLogger(_filePath, categoryName);

    public void Dispose() { }
}
