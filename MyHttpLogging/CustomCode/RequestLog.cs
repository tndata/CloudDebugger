
namespace Microsoft.AspNetCore.MyHttpLogging;

/// <summary>
/// This class contains the latest log entries
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

    public static void ClearLog()
    {
        lock (log)
        {
            log.Clear();
        }
    }

    public static void AddToLog(RequestLogEntry logEntry)
    {
        lock (log)
        {
            if (logEntry != null &&
                CheckForIgnoredPaths(logEntry.Path) == false &&
                CheckForIgnoredExtensions(logEntry.Path) == false)
            {
                LogRequest(logEntry);
            }
        }
    }

    //private static bool CheckForIgnoredResponseTypes(string? responseContentType)
    //{
    //    throw new NotImplementedException();
    //}

    public static RequestLogEntry? LookupLogEntry(int requestNumber)
    {
        return log.FirstOrDefault(x => x.RequestNumber == requestNumber);
    }

    public static List<RequestLogEntry> GetAllLogEntries()
    {
        lock (log)
        {
            return new List<RequestLogEntry>(log);
        }
    }


    private static void LogRequest(RequestLogEntry logEntry)
    {
        //if (context.Request != null && CheckIfWeShouldLogTheBody(context.Request.ContentType))
        //{
        //    entry.RequestBody = context.Request.PeekBody();
        //}

        //if (context.Response != null && CheckIfWeShouldLogTheBody(context.Response.ContentType))
        //{
        //    entry.ResponseBody = new StreamReader(context.Response.Body)?.ReadToEnd() ?? "";
        //}

        logEntry.RequestNumber = requestNumber++;

        log.Add(logEntry);

        //Limit the # of log entries
        if (log.Count > MaxLogEntries)
            log.RemoveAt(0);
    }



    //private static bool CheckIfWeShouldLogTheBody(string? contentType)
    //{
    //    if (contentType == null)
    //        return false;

    //    //// Don't log HTML
    //    //if (contentType.Contains("html"))
    //    //    return false;

    //    // Just log text or json or xml
    //    if (contentType.Contains("text") || contentType.Contains("xml") || contentType.Contains("json"))
    //        return true;

    //    return false;
    //}

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
