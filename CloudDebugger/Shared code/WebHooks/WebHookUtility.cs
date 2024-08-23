using Newtonsoft.Json.Linq;

namespace CloudDebugger.Shared_code.WebHooks
{
    public class WebHookUtility
    {



        public static WebHookLogEntry GetRequestDetails(HttpRequest request, ILogger _logger)
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
                    logEntry.Body = reader.ReadToEndAsync().Result;

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

            logEntry.Headers = new Dictionary<string, string>();
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers.OrderBy(h => h.Key))
            {
                logEntry.Headers.Add(header.Key, header.Value.ToString() ?? "");
            }

            logEntry.HttpMethod = request.Method;
            logEntry.Url = request.Path.Value ?? "";
            return logEntry;
        }


    }
}
