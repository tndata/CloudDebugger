using System.Diagnostics.Tracing;
using System.Text;

namespace CloudDebugger.SharedCode.AzureSdkEventLogger;

/// <summary>
/// Logger for all the events from the Azure SDK
/// </summary>
public static class AzureEventLogger
{
    private readonly static List<EventLogEntry> log = [];

    private readonly static Lock lockObj = new();
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
    /// <param name="evnt"></param>
    public static void AddEventToLog(EventWrittenEventArgs evnt)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Event Details");

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"ActivityId: {evnt.ActivityId}");        //The activity ID on the thread that the event was written to.
        sb.AppendLine($"EventID: {evnt.EventId}");              //The event identifier.
        sb.AppendLine($"EventName: {evnt.EventName}");          //The name of the event.
        sb.AppendLine($"EventSource: {evnt.EventSource.Name}");
        sb.AppendLine($"Keywords: {evnt.Keywords}");            //The keywords for the event.
        sb.AppendLine($"Level: {evnt.Level}");                  //The level of the event.
        sb.AppendLine($"Message: {evnt.Message}");
        sb.AppendLine($"Opcode: {evnt.Opcode}");                //The operation code for the event.

        sb.AppendLine();

        if (evnt.Payload != null)
        {
            sb.AppendLine("Payload");
            for (int i = 0; i < evnt.Payload.Count; i++)
            {
                sb.AppendLine($" - {evnt.PayloadNames?[i] ?? "Null"}: {evnt.Payload[i] ?? "Null"}");
            }
        }

        sb.AppendLine();

        lock (lockObj)
        {

            log.Add(new EventLogEntry()
            {
                EntryTime = DateTime.UtcNow,
                Event = evnt,
                EventType = evnt.EventSource.Name.ToString(),
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
