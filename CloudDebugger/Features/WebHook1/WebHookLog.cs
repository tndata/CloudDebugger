//using CloudDebugger.Shared_code.WebHooks;

//namespace CloudDebugger.Features.WebHook;


///// <summary>
///// Logger class for webhook requests
///// </summary>
//public class WebHookLog
//{
//    private const int MaxLogEntries = 50;
//    private List<WebHookLogEntry> Log = new();

//    public void ClearLog()
//    {
//        lock (Log)
//        {
//            Log.Clear();
//        }
//    }

//    public void AddToLog(WebHookLogEntry logEntry)
//    {
//        lock (Log)
//        {
//            Log.Add(logEntry);

//            //Limit the # of entries
//            if (Log.Count > MaxLogEntries)
//                Log.RemoveAt(0);
//        }
//    }

//    public List<WebHookLogEntry> GetLog()
//    {
//        //Return a new list to avoid any enumeration errors
//        lock (Log)
//        {
//            var log = new List<WebHookLogEntry>(Log);
//        }

//        return Log;
//    }
//}
