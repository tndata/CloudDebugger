namespace CloudDebugger.Features.ConnectionStrings;

public class ConnectionStringsModel
{
    /// <summary>
    /// List of the connection string found
    /// </summary>
    public SortedDictionary<string, string> ConnectionStrings { get; set; } = [];
}