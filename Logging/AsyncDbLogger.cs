using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Data;

namespace SAT242516026.Logging;

public sealed class AsyncDbLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Func<IDbConnection> _connectionFactory;

    public AsyncDbLogger(string categoryName, Func<IDbConnection> connectionFactory)
    {
        _categoryName = categoryName ?? "Unknown";
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
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
            var msg = SafeFormat(state, exception, formatter);

            await using var con = (SqlConnection)_connectionFactory();
            if (con.State != ConnectionState.Open) await con.OpenAsync().ConfigureAwait(false);

            await using var cmd = con.CreateCommand();
            cmd.CommandText =
                @"INSERT INTO dbo.Logs ([Timestamp],[Level],[Category],[Message],[Exception])
                  VALUES (@Timestamp,@Level,@Category,@Message,@Exception)";

            cmd.Parameters.Add(new SqlParameter("@Timestamp", DateTime.Now));
            cmd.Parameters.Add(new SqlParameter("@Level", logLevel.ToString()));
            cmd.Parameters.Add(new SqlParameter("@Category", (object?)_categoryName ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Message", (object?)msg ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Exception", (object?)exception?.ToString() ?? DBNull.Value));

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch
        {
            // yut
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
        catch { }

        return state?.ToString() ?? "";
    }
}

public sealed class AsyncDbLoggerProvider : ILoggerProvider
{
    private readonly Func<IDbConnection> _connectionFactory;

    public AsyncDbLoggerProvider(Func<IDbConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public ILogger CreateLogger(string categoryName)
        => new AsyncDbLogger(categoryName, _connectionFactory);

    public void Dispose() { }
}
