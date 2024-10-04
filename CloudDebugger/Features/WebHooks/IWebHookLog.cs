using CloudDebugger.SharedCode.WebHooks;

namespace CloudDebugger.Features.WebHooks;

public interface IWebHookLog
{
    void AddToLog(WebHookLogEntry logEntry);

    void ClearLog();

    List<WebHookLogEntry> GetAllLogEntries();

    List<WebHookLogEntry> GetLogEntriesForWebHook(int hookId);
}