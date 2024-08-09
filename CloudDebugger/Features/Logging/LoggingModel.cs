namespace CloudDebugger.Features.Logging;

public class LoggingModel
{
    /// <summary>
    /// Message to write to the log
    /// </summary>
    public string? LogMessage { get; set; } = "This is my log message!";

    /// <summary>
    /// Message to the user
    /// </summary>
    public string? Message { get; set; }
}