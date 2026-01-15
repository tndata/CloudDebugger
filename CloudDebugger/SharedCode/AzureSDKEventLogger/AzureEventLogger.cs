using System.Diagnostics.Tracing;
using System.Text;

namespace CloudDebugger.SharedCode.AzureSdkEventLogger;

#pragma warning disable IDE0306
#pragma warning disable IDE0028

/// <summary>
/// Logger for all the events from the Azure SDK
/// </summary>
public static class AzureEventLogger
{
    private readonly static List<EventLogEntry> log = [];

    private readonly static Lock lockObj = new();

    // Limit stored events to prevent unbounded memory growth while keeping enough for debugging
    private const int NumberOfEventsToKeep = 100;

    public static List<EventLogEntry> LogEntries
    {
        get
        {
            lock (lockObj)
            {

                return new List<EventLogEntry>(log);
            }
        }
    }

    /// <summary>
    /// We don't render the message, as its part of the payload.
    /// </summary>
    /// <param name="event"></param>
    public static void AddEventToLog(EventWrittenEventArgs @event)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Event Details");

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"ActivityId: {@event.ActivityId}");        //The activity ID on the thread that the event was written to.
        sb.AppendLine($"EventID: {@event.EventId}");              //The event identifier.
        sb.AppendLine($"EventName: {@event.EventName}");          //The name of the event.
        sb.AppendLine($"EventSource: {@event.EventSource.Name}");
        sb.AppendLine($"Keywords: {@event.Keywords}");            //The keywords for the event.
        sb.AppendLine($"Level: {@event.Level}");                  //The level of the event.
        sb.AppendLine($"Message: {@event.Message}");
        sb.AppendLine($"Opcode: {@event.Opcode}");                //The operation code for the event.

        sb.AppendLine();

        if (@event.Payload != null)
        {
            sb.AppendLine("Payload");
            for (int i = 0; i < @event.Payload.Count; i++)
            {
                sb.AppendLine($" - {@event.PayloadNames?[i] ?? "Null"}: {@event.Payload[i] ?? "Null"}");
            }
        }

        sb.AppendLine();

        lock (lockObj)
        {

            log.Add(new EventLogEntry()
            {
                EntryTime = DateTime.UtcNow,
                Event = @event,
                EventType = @event.EventSource.Name.ToString(),
                Message = sb.ToString()
            });

            if (log.Count > NumberOfEventsToKeep)
                log.RemoveAt(0);
        }
    }

    public static void ClearLog()
    {
        lock (lockObj)
        {
            log.Clear();
        }
    }
}
