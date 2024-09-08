using Microsoft.AspNetCore.MyHttpLogging;

using Microsoft.Extensions.Logging;

namespace MyHttpLogging.CustomCode;

/// <summary>
/// Custom ILogger that only logs HttpLog and all logs are sent to the RequestLog class.
/// 
/// Implement a custom logging provider in .NET
/// https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider
/// 
/// HTTP logging in ASP.NET Core
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-logging/
/// </summary>
public class RequestLogger : ILogger<RequestLogger>
{
    private static List<RequestLogEntry> requestLog = new();
    private const int MaxBodyLength = 5000;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return default!;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        //For now, we only care about HttpLog entries
        switch (state)
        {
            case HttpLog log:
                List<string> requestHeaders = new();
                List<string> responseHeaders = new();
                string? requestBody = "";
                string? responseBody = "";

                string? protocol = "";
                string? method = "";
                string? scheme = "";
                string? pathBase = "";
                string? path = "";
                string? statusCode = "";
                string? duration = "";
                string? responseContentType = "";

                //separate out the various parts in the list of log entries
                foreach (var entry in log)
                {
                    var key = entry.Key;

                    if (key.StartsWith("request-"))
                    {
                        key = key.Replace("request-", "");
                        requestHeaders.Add(key + ": " + entry.Value);
                    }
                    if (key.StartsWith("response-"))
                    {
                        key = key.Replace("response-", "");
                        responseHeaders.Add(key + ": " + entry.Value);

                        if (key == "Content-Type")
                            responseContentType = entry.Value?.ToString();
                    }

                    switch (key)
                    {
                        case "RequestBody":
                            requestBody = entry.Value?.ToString();
                            break;
                        case "ResponseBody":
                            responseBody = entry.Value?.ToString();
                            break;
                        case "Protocol":
                            protocol = entry.Value?.ToString();
                            break;
                        case "Method":
                            method = entry.Value?.ToString();
                            break;
                        case "Scheme":
                            scheme = entry.Value?.ToString();
                            break;
                        case "Path":
                            path = entry.Value?.ToString();
                            break;
                        case "PathBase":
                            pathBase = entry.Value?.ToString();
                            break;
                        case "StatusCode":
                            statusCode = entry.Value?.ToString();
                            break;
                        case "Duration":
                            duration = entry.Value?.ToString() + " ms";
                            break;
                    }
                }

                if (requestBody != null && requestBody.Length > MaxBodyLength)
                    requestBody = requestBody.Substring(0, MaxBodyLength) + "...[truncated]...";

                if (responseBody != null && responseBody.Length > MaxBodyLength)
                    responseBody = responseBody.Substring(0, MaxBodyLength) + "...[truncated]...";


                //Populate the object
                var requestLogEntry = new RequestLogEntry
                {
                    Protocol = protocol,
                    Method = method,
                    EntryTimeUtc = DateTime.UtcNow,
                    Path = path,
                    PathBase = pathBase,
                    RequestHeaders = requestHeaders,
                    ResponseHeaders = responseHeaders,
                    RequestBody = requestBody,
                    ResponseBody = responseBody,
                    StatusCode = statusCode,
                    Duration = duration,
                    ResponseContentType = responseContentType
                };

                RequestLog.AddToLog(requestLogEntry);

                break;
            default:
                // Ignore all other log entries
                break;
        }
    }
}
