using CloudDebugger.SharedCode.WebHooks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace CloudDebugger.Features.WebHooks;

public static class WebHookUtility
{
    /// <summary>
    /// Get the request details and package it up into a WebHookLogEntry object.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public async static Task<WebHookLogEntry> GetRequestDetails(int hookId, HttpRequest request, ILogger logger)
    {
        var logEntry = new WebHookLogEntry()
        {
            HookId = hookId,
            EntryTime = DateTime.UtcNow,
            Comment = "",
            HttpMethod = request.Method,
            Url = request.Path.Value ?? "",
            IsJSON = false
        };

        if (request.Headers.ContainsKey("Content-Type"))
        {
            logEntry.ContentType = request.Headers.ContentType.FirstOrDefault() ?? "[Unknown]";
        }

        //Get request body 
        logEntry.Body = "";
        if (request.ContentLength > 0)
        {
            // Get the body
            using (var reader = new StreamReader(request.Body))
            {
                logEntry.Body = await reader.ReadToEndAsync();
            }

            if (!string.IsNullOrEmpty(logEntry.Body) && logEntry.ContentType != null)
            {
                if (logEntry.ContentType.Contains("json"))
                {
                    try
                    {
                        // Make the JSON pretty.
                        var obj = JToken.Parse(logEntry.Body);

                        logEntry.IsJSON = true;
                        logEntry.Body = obj.ToString();

                        // Extract the subject field
                        JToken? subjectToken = null;

                        if (obj.Type == JTokenType.Array)
                        {
                            var firstItem = obj.First;
                            subjectToken = firstItem?.SelectToken("subject");
                        }
                        else if (obj.Type == JTokenType.Object)
                        {
                            subjectToken = obj.SelectToken("subject");
                        }

                        if (subjectToken != null)
                        {
                            logEntry.Subject = subjectToken.ToString();
                            logger.LogInformation("Received cloud event with subject='{Subject}':", logEntry.Subject);
                        }
                    }
                    catch (Exception ex)
                    {
                        // If JSON parsing fails, keep the original result, ignore invalid JSON
                        logEntry.Comment = "Invalid JSON " + ex.Message;
                    }
                }

                if (logEntry.ContentType.Contains("x-www-form-urlencoded"))
                {
                    try
                    {
                        var sb = new StringBuilder();

                        string decodedString = WebUtility.UrlDecode(logEntry.Body);

                        // Parse the URL-encoded string
                        var queryParameters = HttpUtility.ParseQueryString(decodedString);

                        foreach (string? key in queryParameters.AllKeys)
                        {
                            if (key != null)
                                sb.AppendLine($"{key}: {queryParameters[key]}");
                        }

                        //Append the parameters to the end
                        logEntry.Body += $"\r\n\r\nDecoded parameters\r\n{sb}\r\n";
                    }
                    catch
                    {
                        // If JSON parsing fails, keep the original result, ignore invalid data
                        logEntry.Comment = "Invalid x-www-form-urlencoded body";
                    }
                }
            }
        }

        //Get an ordered list of alll the request headers
        logEntry.RequestHeaders = [];
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers.OrderBy(h => h.Key))
        {
            logEntry.RequestHeaders.Add(header.Key, header.Value.ToString() ?? "");
        }

        return logEntry;
    }
}
