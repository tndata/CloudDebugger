using CloudDebugger.SharedCode.AzureSDKEventLogger;

namespace CloudDebugger.Features.AzureSDKEventLogViewer;

public class ViewLogModel
{
    public List<EventLogEntry>? Log { get; set; }
}