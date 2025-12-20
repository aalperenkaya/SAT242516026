using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SAT242516026.Logging;

public class LogService(string filePath, Func<IDbConnection> connectionFactory)
{
    public async Task<List<LogEntry>> GetFileLogsAsync()
    {
        var logs = new List<LogEntry>();
        if (!File.Exists(filePath))
            return logs;

        var lines = await File.ReadAllLinesAsync(filePath);
        var regex = new Regex(@"^(?<date>[\d\-T:\.Z ]+) \[(?<level>\w+)\] (?<category>[^:]+): (?<message>.*)$");

        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                logs.Add(new LogEntry
                {
                    Timestamp = DateTime.TryParse(
                        match.Groups["date"].Value,
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AssumeLocal,
                        out var dt
                    ) ? dt : DateTime.UtcNow,
                    Level = match.Groups["level"].Value,
                    Category = match.Groups["category"].Value,
                    Message = match.Groups["message"].Value,
                    Source = "File"
                });
            }
        }

        logs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
        return logs;
    }

    public async Task<List<LogEntry>> GetDbLogsAsync()
    {
        var logs = new List<LogEntry>();

        try
        {
            await using var connection = (SqlConnection)connectionFactory();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Timestamp, Level, Category, Message, Exception FROM Logs ORDER BY Timestamp DESC";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new LogEntry
                {
                    Timestamp = reader.GetDateTime(0),
                    Level = reader.GetString(1),
                    Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Message = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Exception = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Source = "Database"
                });
            }
        }
        catch (Exception ex)
        {
            logs.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Error",
                Message = $"Veritabanı logları okunamadı: {ex.Message}",
                Source = "System"
            });
        }

        return logs;
    }

    public async Task<List<LogEntry>> GetLogsAsync()
    {
        var fileLogs = await GetFileLogsAsync();
        var dbLogs = await GetDbLogsAsync();

        var combined = new List<LogEntry>();
        combined.AddRange(fileLogs);
        combined.AddRange(dbLogs);

        combined.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
        return combined;
    }
}
