namespace CloudDebugger.Features.OpenTelemetryLog;

public class ViewLogsModel
{
    public List<LogEntry>? Entries { get; set; }
}

public class LogEntry
{
    public DateTime Time { get; set; }
    public string? Subject { get; set; }
    public string? Data { get; set; }
}