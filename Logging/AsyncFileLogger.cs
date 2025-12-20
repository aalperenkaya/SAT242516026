namespace SAT242516026.Logging;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;

    public class AsyncFileLogger(string filePath, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public async void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = $"{DateTime.Now} [{logLevel}] {categoryName}: {formatter(state, exception)}{Environment.NewLine}";
            try
            {
                await File.AppendAllTextAsync(filePath, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AsyncFileLogger error: {ex.Message}");
            }
        }
    }

    public class AsyncFileLoggerProvider(string filePath) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
            => new AsyncFileLogger(filePath, categoryName);

        public void Dispose() { }
    }
