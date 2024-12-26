using CloudDebugger.SharedCode.AzureSdkEventLogger;

namespace CloudDebugger.Features.AzureSdkEventLogViewer;

public class ViewLogModel
{
    public List<EventLogEntry>? Log { get; set; }
}