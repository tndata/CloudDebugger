namespace Azure.MyIdentity
{
    /// <summary>
    /// This is a custom hack that incorporates a simple log into this library.
    /// </summary>
    public static class MyAzureIdentityLog
    {
        public readonly static List<AzureIdentityLogEntry> Log = new();
        private readonly static object lockObj = new();

        public static void AddToLog(string context, string message)
        {
            lock (lockObj)
            {

                Log.Add(new AzureIdentityLogEntry()
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
            lock (lockObj)
            {
                Log.Clear();
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
