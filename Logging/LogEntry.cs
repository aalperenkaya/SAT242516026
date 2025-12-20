namespace SAT242516026.Logging
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string? Level { get; set; }
        public string? Category { get; set; }
        public string? Message { get; set; }
        public string? Exception { get; set; }
        public string Source { get; set; } = "Unknown";
    }
}