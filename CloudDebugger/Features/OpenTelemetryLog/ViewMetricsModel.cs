namespace CloudDebugger.Features.OpenTelemetryLog;

public class ViewMetricsModel
{
    public List<MetricsEntry>? Entries { get; set; }
}

public class MetricsEntry
{
    public DateTime Time { get; set; }
    public string? Subject { get; set; }
    public string? Data { get; set; }
}