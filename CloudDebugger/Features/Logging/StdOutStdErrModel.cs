namespace CloudDebugger.Features.Logging;

public class StdOutStdErrModel
{
    /// <summary>
    /// Message to write to the standard out
    /// </summary>
    public string? StdOutMessage { get; set; }

    /// <summary>
    /// Message to write to the standard error
    /// </summary>
    public string? StdErrMessage { get; set; }

    public string? Exception { get; set; }
    public string? Message { get; set; }
}

