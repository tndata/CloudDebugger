using Newtonsoft.Json.Linq;

namespace CloudDebugger.Shared_code.WebHooks;

public class WebHookUtility
{

    /// <summary>
    /// Get the request and package it up into a WebHookLogEntry
    /// </summary>
    /// <param name="request"></param>
    /// <param name="_logger"></param>
    /// <returns></returns>
    public async static Task<WebHookLogEntry> GetRequestDetails(HttpRequest request, ILogger _logger)
    {
        var logEntry = new WebHookLogEntry();

        logEntry.EntryTime = DateTime.Now;
        logEntry.Comment = "";

        if (request.Headers.ContainsKey("Content-Type"))
        {
            logEntry.ContentType = request.Headers["Content-Type"].FirstOrDefault() ?? "[Unknown]";
        }

        //Get request body 
        logEntry.Body = "";
        if (request.ContentLength > 0)
        {
            using (var reader = new StreamReader(request.Body))
            {
                logEntry.Body = await reader.ReadToEndAsync();

                //Optionally, parse the JSON body to make it pretty.
                if (logEntry.ContentType != null && logEntry.ContentType.Contains("json"))
                {
                    try
                    {
                        var obj = JToken.Parse(logEntry.Body);
                        logEntry.Body = obj.ToString();

                        // Get the subject if present
                        dynamic dynObj = obj;
                        if (dynObj != null)
                        {
                            logEntry.Subject = dynObj.subject ?? "[Subject missing]";

                            _logger.LogInformation("Received cloud event with subject:" + logEntry.Subject);
                        }
                    }
                    catch
                    {
                        // If parsing fails, keep the original result
                    }
                }
            }
        }

        logEntry.RequestHeaders = new Dictionary<string, string>();
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers.OrderBy(h => h.Key))
        {
            logEntry.RequestHeaders.Add(header.Key, header.Value.ToString() ?? "");
        }

        logEntry.HttpMethod = request.Method;
        logEntry.Url = request.Path.Value ?? "";
        return logEntry;
    }


}
