namespace Azure.MyIdentity
{
    public class MyAzureIdentityLog
    {
        public static List<LogEntry> Log = new();

        public static void AddToLog(string context, string message)
        {
            lock (Log)
            {

                Log.Add(new LogEntry()
                {
                    EntryTime = DateTime.Now,
                    Context = context,
                    Message = message
                });

                if (Log.Count > 500)
                    Log.RemoveAt(0);
            }
        }

        public static void ClearLog()
        {
            lock (Log)
            {
                Log.Clear();
            }
        }
    }

    public class LogEntry
    {
        public DateTime EntryTime { get; set; }
        public string Context { get; set; }
        public string Message { get; set; }
    }
}
