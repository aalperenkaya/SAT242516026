namespace SAT242516026.Logging;
using Microsoft.Extensions.Logging;

public class CompositeLogger(IEnumerable<ILogger> loggers) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        foreach (var logger in loggers)
        {
            try
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompositeLogger sublogger failed: {ex.Message}");
            }
        }
    }
}

public class CompositeLoggerProvider : ILoggerProvider
{
    private readonly List<ILoggerProvider> _providers = new();

    public CompositeLoggerProvider AddProvider(ILoggerProvider provider)
    {
        _providers.Add(provider);
        return this;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var loggers = new List<ILogger>();
        foreach (var provider in _providers)
            loggers.Add(provider.CreateLogger(categoryName));

        return new CompositeLogger(loggers);
    }

    public void Dispose()
    {
        foreach (var p in _providers)
            p.Dispose();
    }
}