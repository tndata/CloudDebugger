namespace Microsoft.AspNetCore.MyHttpLogging;

/// <summary>
/// Request log entry
/// </summary>
public class RequestLogEntry
{
    public int RequestNumber { get; set; }
    public string? Protocol { get; set; }
    public string? Method { get; set; }
    public DateTime EntryTime { get; set; }
    public string? Path { get; set; }
    public string? PathBase { get; set; }
    public List<string> RequestHeaders { get; set; } = new();
    public List<string> ResponseHeaders { get; set; } = new();
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? StatusCode { get; set; }
    public string? Duration { get; set; }
    public string? ResponseContentType { get; set; }
}
