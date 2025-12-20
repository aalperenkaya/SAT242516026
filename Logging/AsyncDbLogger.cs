namespace SAT242516026.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Data;

public class AsyncDbLogger(string categoryName, Func<IDbConnection> connectionFactory) : ILogger
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

        try
        {
            await using var connection = (SqlConnection)connectionFactory();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                @"INSERT INTO Logs (Timestamp, Level, Category, Message, Exception)
                  VALUES (@Timestamp, @Level, @Category, @Message, @Exception)";

            command.Parameters.Add(new SqlParameter("@Timestamp", DateTime.Now));
            command.Parameters.Add(new SqlParameter("@Level", logLevel.ToString()));
            command.Parameters.Add(new SqlParameter("@Category", categoryName ?? (object)DBNull.Value));
            command.Parameters.Add(new SqlParameter("@Message", formatter(state, exception) ?? (object)DBNull.Value));
            command.Parameters.Add(new SqlParameter("@Exception", exception?.ToString() ?? (object)DBNull.Value));

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AsyncDbLogger error: {ex.Message}");
        }
    }
}

public class AsyncDbLoggerProvider(Func<IDbConnection> connectionFactory) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new AsyncDbLogger(categoryName, connectionFactory);

    public void Dispose() { }
}