namespace CloudDebugger.Shared_code.WebHooks;

public class WebHookLogEntry
{
    public int HookId { get; set; }
    public DateTime EntryTime { get; set; }

    public string? Url { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }

    public string? HttpMethod { get; set; }
    public string? ContentType { get; set; }
    public string? Comment { get; set; }

    public Dictionary<string, string> RequestHeaders { get; set; } = new();
}