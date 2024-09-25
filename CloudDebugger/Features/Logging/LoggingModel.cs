namespace CloudDebugger.Features.Logging;

public class LoggingModel
{
    /// <summary>
    /// Message to write to the log
    /// </summary>
    public string? LogMessage { get; set; }

    /// <summary>
    /// The desired log category (typically the fully qualified type name, like MyApp.Controllers.HomeController)
    /// </summary>
    public string? LogCategory { get; set; }

    public string? Exception { get; set; }
    public string? Message { get; set; }
}