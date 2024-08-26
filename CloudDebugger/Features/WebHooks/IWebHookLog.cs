using CloudDebugger.Shared_code.WebHooks;

namespace CloudDebugger.Features.WebHooks
{
    public interface IWebHookLog
    {
        void AddToLog(int hookId, WebHookLogEntry logEntry);
        void ClearLog();
        List<WebHookLogEntry> GetAllLogEntries();
        List<WebHookLogEntry> GetLogEntriesForWebHook(int HookId);
    }
}