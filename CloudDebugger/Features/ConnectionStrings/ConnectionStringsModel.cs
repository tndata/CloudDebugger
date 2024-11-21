namespace CloudDebugger.Features.ConnectionStrings;

public class ConnectionStringsModel
{
    /// <summary>
    /// Message to write to the log
    /// </summary>
    public SortedDictionary<string, string> ConnectionStrings { get; set; } = [];
}