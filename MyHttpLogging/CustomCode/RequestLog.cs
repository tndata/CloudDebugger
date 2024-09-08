namespace Microsoft.AspNetCore.MyHttpLogging;

/// <summary>
/// This custom class contains the latest log entries
/// </summary>
public static class RequestLog
{
    private static readonly List<string> pathsToIgnore = new()
        {
            "/favicon.ico", "/robots.txt", "/requestlog"
        };

    private static readonly List<string> extensionsToIgnore = new()
        {
            ".css",".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot", ".map"
        };

    private const int MaxLogEntries = 50;
    private static int requestNumber = 1;

    private static List<RequestLogEntry> log = new();
    private static readonly object lockObj = new();

    public static void ClearLog()
    {
        lock (lockObj)
        {
            log.Clear();
        }
    }

    public static void AddToLog(RequestLogEntry logEntry)
    {
        lock (lockObj)
        {
            if (logEntry != null &&
                CheckForIgnoredPaths(logEntry.Path) == false &&
                CheckForIgnoredExtensions(logEntry.Path) == false)
            {
                LogRequest(logEntry);
            }
        }
    }

    public static RequestLogEntry? LookupLogEntry(int requestNumber)
    {
        return log.FirstOrDefault(x => x.RequestNumber == requestNumber);
    }

    public static List<RequestLogEntry> GetAllLogEntries()
    {
        lock (lockObj)
        {
            return new List<RequestLogEntry>(log);
        }
    }

    private static void LogRequest(RequestLogEntry logEntry)
    {
        logEntry.RequestNumber = requestNumber++;

        log.Add(logEntry);

        //Limit the # of log entries
        if (log.Count > MaxLogEntries)
            log.RemoveAt(0);
    }

    private static bool CheckForIgnoredPaths(string? path)
    {
        if (path == null)
            return false;

        foreach (var pathToIgnore in pathsToIgnore)
        {
            if (path.ToLower().StartsWith(pathToIgnore))
                return true;
        }

        return false;
    }

    private static bool CheckForIgnoredExtensions(string? path)
    {
        if (path == null)
            return false;

        var pathToLower = path.ToLower();

        foreach (var extension in extensionsToIgnore)
        {
            if (pathToLower.EndsWith(extension))
                return true;
        }

        return false;
    }
}
