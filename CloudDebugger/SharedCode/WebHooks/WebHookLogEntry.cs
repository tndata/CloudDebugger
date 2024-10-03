namespace CloudDebugger.SharedCode.WebHooks;

public class WebHookLogEntry
{
    public int HookId { get; set; }

    /// <summary>
    /// When the request was received.
    /// </summary>
    public DateTime EntryTime { get; set; }

    /// <summary>
    /// The URL of the request.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The optional event message subject field if present. 
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The raw body of the request.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// True if the body of the request is JSON
    /// </summary>
    public bool IsJSON { get; set; }

    /// <summary>
    /// HTTP method
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request content type
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Optional comment about this request/entry.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// All the request headers
    /// </summary>
    public Dictionary<string, string> RequestHeaders { get; set; } = [];
}