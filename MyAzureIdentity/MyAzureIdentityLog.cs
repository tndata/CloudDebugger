namespace Azure.MyIdentity
{
    /// <summary>
    /// This is a custom hack that incorporates a simple log into this library.
    /// </summary>
    public static class MyAzureIdentityLog
    {
        private readonly static List<AzureIdentityLogEntry> log = new();
        private readonly static object lockObj = new();

        public static List<AzureIdentityLogEntry> LogEntries
        {
            get
            {
                lock (lockObj)
                {
                    return new List<AzureIdentityLogEntry>(log);
                }
            }
        }

        public static void AddToLog(string context, string message)
        {
            lock (lockObj)
            {

                log.Add(new AzureIdentityLogEntry()
                {
                    EntryTime = DateTime.UtcNow,
                    Context = context,
                    Message = message
                });

                if (log.Count > 500)
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

    public class AzureIdentityLogEntry
    {
        public DateTime EntryTime { get; set; }
        public string Context { get; set; }
        public string Message { get; set; }
    }
}
