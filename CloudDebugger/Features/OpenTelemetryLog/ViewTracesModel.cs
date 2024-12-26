namespace CloudDebugger.Features.OpenTelemetryLog;

public class ViewTracesModel
{
    public List<TraceEntry>? Entries { get; set; }
}

public class TraceEntry
{
    public DateTime Time { get; set; }
    public string? Subject { get; set; }
    public string? Data { get; set; }
}