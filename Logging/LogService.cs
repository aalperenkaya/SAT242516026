using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SAT242516026.Logging;

public sealed class LogService(string filePath, Func<IDbConnection> connectionFactory)
{
    public async Task<List<LogEntry>> GetFileLogsAsync()
    {
        var logs = new List<LogEntry>();

        try
        {
            if (!File.Exists(filePath))
                return logs;

            var lines = await File.ReadAllLinesAsync(filePath);

            // "2025-12-27 11:22:33 [Information] Category: msg"
            var regex = new Regex(
                @"^(?<date>[\d\-: ]+)\s\[(?<level>[^\]]+)\]\s(?<category>[^:]+):\s(?<message>.*)$",
                RegexOptions.Compiled);

            foreach (var line in lines)
            {
                var m = regex.Match(line);
                if (!m.Success) continue;

                _ = DateTime.TryParse(
                    m.Groups["date"].Value,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AssumeLocal,
                    out var dt);

                logs.Add(new LogEntry
                {
                    Timestamp = dt == default ? DateTime.Now : dt,
                    Level = m.Groups["level"].Value,
                    Category = m.Groups["category"].Value,
                    Message = m.Groups["message"].Value,
                    Source = "File"
                });
            }

            logs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            return logs;
        }
        catch (Exception ex)
        {
            logs.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = "Error",
                Category = "LogService",
                Message = $"File log okunamadı: {ex.Message}",
                Source = "System"
            });
            return logs;
        }
    }

    public async Task<List<LogEntry>> GetDbLogsAsync()
    {
        var logs = new List<LogEntry>();

        try
        {
            await using var con = (SqlConnection)connectionFactory();
            if (con.State != ConnectionState.Open)
                await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandText =
                @"SELECT [Timestamp],[Level],[Category],[Message],[Exception]
                  FROM dbo.Logs
                  ORDER BY [Timestamp] DESC";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new LogEntry
                {
                    Timestamp = reader.GetDateTime(0),
                    Level = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Message = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Exception = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Source = "Database"
                });
            }

            return logs;
        }
        catch (Exception ex)
        {
            logs.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = "Error",
                Category = "LogService",
                Message = $"Veritabanı logları okunamadı: {ex.Message}",
                Source = "System"
            });

            return logs;
        }
    }

    public async Task<List<LogEntry>> GetLogsAsync()
    {
        var combined = new List<LogEntry>();
        combined.AddRange(await GetFileLogsAsync());
        combined.AddRange(await GetDbLogsAsync());
        combined.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
        return combined;
    }
}
