namespace CloudDebugger.Features.WebHook;

public class WebHookLogEntry
{
    public DateTime EntryTime { get; set; }
    public string? HttpMethod { get; set; }
    public string? ContentType { get; set; }
    public string? Comment { get; set; }
    public string? Url { get; set; }
    public string? Body { get; set; }
    public string? Subject { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}