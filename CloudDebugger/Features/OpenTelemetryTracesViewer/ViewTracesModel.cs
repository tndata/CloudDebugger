namespace CloudDebugger.Features.OpenTelemetryTracesViewer;

public class ViewTracesModel
{
    public List<TraceEntry>? Entries { get; set; }
    public string? TraceId { get; set; }
}

public class TraceEntry
{
    public DateTime Time { get; set; }
    public string? Subject { get; set; }
    public string? Data { get; set; }
}