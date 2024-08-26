using CloudDebugger.Shared_code.WebHooks;

namespace CloudDebugger.Features.WebHooks;

/// <summary>
/// WebHooks logging class. It will rememeber the last 100 webhook requests in the log.
/// </summary>
public class WebHookLog : IWebHookLog
{
    private const int MaxLogEntries = 100;
    private List<WebHookLogEntry> Log = new();

    public WebHookLog()
    {
    }

    public void ClearLog()
    {
        lock (Log)
        {
            Log.Clear();
        }
    }

    public void AddToLog(int hookId, WebHookLogEntry logEntry)
    {
        lock (Log)
        {
            logEntry.HookId = hookId;
            Log.Add(logEntry);

            //Limit the # of entries
            if (Log.Count > MaxLogEntries)
                Log.RemoveAt(0);
        }
    }

    public List<WebHookLogEntry> GetAllLogEntries()
    {
        //Return a new list to avoid any enumeration errors
        lock (Log)
        {
            var log = new List<WebHookLogEntry>(Log);
        }

        return Log;
    }


    public List<WebHookLogEntry> GetLogEntriesForWebHook(int hookId)
    {
        //Return a new list to avoid any enumeration errors
        lock (Log)
        {
            var filtered = Log.Where(e => e.HookId == hookId).ToList();
            return filtered;
            var log = new List<WebHookLogEntry>(filtered.ToList());
        }
    }

}

