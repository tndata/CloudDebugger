using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace CloudDebugger.SharedCode.WebHooks;

/// <summary>
/// WebHook Validation logic
/// 
/// Some services require the webhook endpoint to respond to a validation request before they begin sending events to it.
/// </summary>
public static class WebHookValidation
{
    /// <summary>
    /// Test if this is a EventGrid Event Subscription validation request
    /// Inspiration taken from https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventgrid/Microsoft.Azure.WebJobs.Extensions.EventGrid/src/TriggerBinding/HttpRequestProcessor.cs
    /// More details about the request https://learn.microsoft.com/en-us/azure/event-grid/receive-events
    /// https://learn.microsoft.com/en-us/azure/event-grid/end-point-validation-event-grid-events-schema
    /// https://github.com/Azure-Samples/event-grid-dotnet-publish-consume-events/blob/master/EventGridConsumer/EventGridConsumer/Function1.cs
    /// </summary>
    /// <param name="webHookLog"></param>
    /// <param name="logEntry"></param>
    public static void CheckIfEventGridSchemaValdationRequest(Action<WebHookLogEntry> LogEventHandler, WebHookLogEntry logEntry, ILogger logger)
    {
        if (logEntry.RequestHeaders.TryGetValue("aeg-event-type", out string? value))
        {
            var eventType = value;

            switch (eventType)
            {
                case "SubscriptionValidation":
                    HandleEventGridSubscriptionValidation(LogEventHandler, logEntry, logger);
                    break;
                case "Notification":
                    //Do nothing
                    break;
                case "SubscriptionDeletion":
                    //Do nothing
                    break;
                default:
                    break;
            }
        }
    }


    /// <summary>
    /// Test if this is a EventGrid Cloud Event Subscription validation request.
    /// https://learn.microsoft.com/en-gb/azure/event-grid/receive-events
    /// https://github.com/cloudevents/spec/blob/v1.0/http-webhook.md#4-abuse-protection 
    /// https://stackoverflow.com/questions/59622185/azure-event-grid-subscription-webhook-validation-fails-with-cloud-events-schema
    /// </summary>
    /// <param name="webHookLog"></param>
    /// <param name="logEntry"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal static void CheckIfCloudEventValidationRequest(HttpContext httpContext, Action<WebHookLogEntry> LogEventHandler, WebHookLogEntry logEntry, ILogger logger)
    {

        logEntry.RequestHeaders.TryGetValue("WebHook-Request-Callback", out string? callbackUrl);
        logEntry.RequestHeaders.TryGetValue("WebHook-Request-Origin", out string? callbackorigin);

        if (!string.IsNullOrEmpty(callbackUrl))
        {
            //Respond with the correct headers and body
            httpContext.Response.Headers.Append("WebHook-Request-Origin", "eventgrid.azure.net");
            httpContext.Response.Headers.Append("WebHook-Allowed-Rate", "120");

            logger.LogInformation("Received CLoudEvent  validation request with callbackUrl '{CallbackUrl}' and callbackorigin '{CallbackOrigin}'", callbackUrl, callbackorigin);

            SendCallBackRequest(logEntry.HookId, LogEventHandler, callbackUrl, "Event Grid Cloud events webhook confirmation request.");
        }
    }



    private static void HandleEventGridSubscriptionValidation(Action<WebHookLogEntry> LogEventHandler, WebHookLogEntry logEntry, ILogger logger)
    {
        if (logEntry.Body != null && logEntry.Body.Length > 0)
        {
            JToken receivedEvents;
            try
            {
                receivedEvents = JToken.Parse(logEntry.Body);
            }
            catch
            {
                // If parsing fails, just exit
                return;
            }

            string? validationCode = null;
            string? validationUrl = null;
            switch (receivedEvents.Type)
            {
                case JTokenType.Array:
                    validationCode = receivedEvents[0]?["data"]?["validationCode"]?.ToString();
                    validationUrl = receivedEvents[0]?["data"]?["validationUrl"]?.ToString();

                    logger.LogInformation("Received EventGrid subscription validation request with validation code '{ValidationCode}' and validation URL '{ValidationUrl}'", validationCode, validationUrl);
                    break;
                case JTokenType.Object:
                    {
                        // The Data is double-encoded in the CloudEvent subscription event
                        var data = JToken.Parse(receivedEvents["data"]?.ToString() ?? "");
                        validationCode = data["validationCode"]?.ToString();
                        validationUrl = data["validationUrl"]?.ToString();

                        logger.LogInformation("Received EventGrid subscription validation request with validation code '{ValidationCode}' and validation URL '{ValidationUrl}'", validationCode, validationUrl);
                        break;
                    }
                case JTokenType.Boolean:
                case JTokenType.Bytes:
                case JTokenType.Comment:
                case JTokenType.Constructor:
                case JTokenType.Date:
                case JTokenType.Float:
                case JTokenType.Guid:
                case JTokenType.Integer:
                case JTokenType.None:
                case JTokenType.Null:
                case JTokenType.Property:
                case JTokenType.Raw:
                case JTokenType.String:
                case JTokenType.TimeSpan:
                case JTokenType.Undefined:
                case JTokenType.Uri:
                    // Do nothing for these cases
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        $"The request content should be parseable into a JSON object or array, but was {receivedEvents.Type}.");
            }


            if (validationUrl != null)
            {
                SendCallBackRequest(logEntry.HookId, LogEventHandler, validationUrl, "Event Grid schema webhook confirmation request.");
            }
        }
    }




    private static void SendCallBackRequest(int hookId, Action<WebHookLogEntry> LogEventHandler, string callbackUrl, string comment)
    {
        //Call the callBackUrl to confirm the webhoook
        ThreadPool.QueueUserWorkItem(delegate (object? state)
        {
            // We send the callback on a separate thread, slightly delayed, so that the callback will be accepted.
            // It is important to send this request after the current request is completed, otherwise it will not be accepted. 
            Thread.Sleep(5000);

            var newLogEntry = new WebHookLogEntry()
            {
                HookId = hookId,
                EntryTime = DateTime.UtcNow,
                Url = callbackUrl,
                HttpMethod = "GET",
                Comment = comment
            };

            try
            {
                var client = new HttpClient();

                // Create the request
                var request = new HttpRequestMessage(HttpMethod.Get, callbackUrl);
                // Send the request
                var response = client.SendAsync(request).Result;

                // Ensure to dispose the request after it's used
                request.Dispose();

                //TODO: Retry the request if the response body is empty, can happen from time to time. Response body: ''

                // Read the response content as string
                var result = response.Content.ReadAsStringAsync().Result;

                // Determine Content Type and pretty-print response JSON if applicable
                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    try
                    {
                        // Attempt to parse and pretty-print JSON
                        var parsedJson = JToken.Parse(result).ToString(Formatting.Indented);
                        result = parsedJson;
                    }
                    catch
                    {
                        // If parsing fails, keep the original result
                    }
                }

                // Check if the response was successful
                if (response.IsSuccessStatusCode)
                {
                    StringBuilder logEntry = new();

                    logEntry.AppendLine($"Called the webhook validation URL:");
                    logEntry.AppendLine($"{callbackUrl}");
                    logEntry.AppendLine();
                    logEntry.AppendLine($"Response status code: {(int)response.StatusCode} ({response.StatusCode})");
                    logEntry.AppendLine();
                    logEntry.AppendLine("Response headers:");

                    //Add the response headers to the log
                    foreach (var header in response.Headers)
                    {
                        logEntry.AppendLine($"{header.Key}: {string.Join(",", header.Value)}");
                    }

                    if (result.Length > 2000)
                    {
                        result = result.Substring(0, 2000) + "...";
                    }

                    logEntry.AppendLine("\r\nResponse body:");
                    logEntry.AppendLine($"\r\n{result}\r\n");

                    newLogEntry.Body = logEntry.ToString();
                }
                else
                {
                    // Handle non-success status codes
                    newLogEntry.Body = $"Webhook validation URL call failed with status code {response.StatusCode}.\r\n\r\nResponse body:\r\n\r\n'{result}'";
                }
            }
            catch (HttpRequestException exc)
            {
                // Handle the case where the request couldn't be sent or no response was received
                newLogEntry.Body = $"\r\n### Exception: {exc.Message}\r\nCalling the webhook validation URL failed. No response was received.";
            }
            catch (Exception exc)
            {
                // Handle other exceptions
                newLogEntry.Body = $"\r\n### Exception: {exc.Message}\r\nAn unexpected error occurred while calling the webhook validation URL.";
            }

            // Add an extra entry in the log about this validation request.
            LogEventHandler(newLogEntry);
        });
    }
}