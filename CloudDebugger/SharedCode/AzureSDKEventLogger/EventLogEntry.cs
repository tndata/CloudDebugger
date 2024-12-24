using System.Diagnostics.Tracing;

namespace CloudDebugger.SharedCode.AzureSDKEventLogger;

public class EventLogEntry
{
    /// <summary>
    /// The time the log entry was created.
    /// </summary>
    public DateTime EntryTime { get; set; }

    /// <summary>
    /// Details associated with the log entry.
    /// </summary>
    public EventWrittenEventArgs? Event { get; set; }

    /// <summary>
    /// The type of the event
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Message associated with the log entry.
    /// </summary>
    public string? Message { get; set; }
}
