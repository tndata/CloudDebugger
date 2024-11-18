using CloudDebugger.SharedCode.WebHooks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace CloudDebugger.Features.WebHooks;

/// <summary>
/// These are the four public webhook endpoints that accepts requests from other services.
/// 
/// /Hook1
/// /Hook2
/// /Hook3
/// /Hook4
/// 
/// These four endpoints will do the following:
/// * Accept HTTP GET,PUT, POST, DELETE And OPTIONS requests
/// * Get details about the request and then add it to the log
/// * Optionally, send a callback request back to the caller for validation purposes
/// * Supports Cloud events and Event Grid schema validation
/// 
/// Options:
/// * WebHookFailureEnable
/// </summary>
[EnableCors("MyCorsPolicy_wildcard")]
[Route("/")]
[ApiController]
public class WebHookApiController : ControllerBase
{
    private readonly ILogger<WebHookApiController> logger;
    private readonly IWebHookLog webHookLog;
    private readonly IHubContext<WebHookHub> signalRhubContext;
    private static int WebHookCounter;

    public WebHookApiController(ILogger<WebHookApiController> logger,
                                IWebHookLog webHookLog,
                                IHubContext<WebHookHub> hubContext)
    {
        this.logger = logger;
        this.webHookLog = webHookLog;
        this.signalRhubContext = hubContext;
    }

    /// <summary>
    /// Accepts requests to the following endpoints:
    /// 
    /// /Hook1
    /// /Hook2
    /// /Hook3
    /// /Hook4
    /// </summary>
    /// <returns></returns>
    [HttpGet("hook{id:int:min(1):max(4)}")]
    [HttpPut("hook{id:int:min(1):max(4)}")]
    [HttpPost("hook{id:int:min(1):max(4)}")]
    [HttpDelete("hook{id:int:min(1):max(4)}")]
    [HttpOptions("hook{id:int:min(1):max(4)}")]
    public Task<IActionResult> Hook(int id)
    {
        return ProcessHook(id);
    }


    private async Task<IActionResult> ProcessHook(int hookId)
    {
        try
        {
            var hookRequest = await WebHookUtility.GetRequestDetails(hookId, Request, logger);

            if (WebHookSettings.WebHookFailureEnabled)
            {
                // Simulate a webhook failure by returning a HTTP 500 Server Errror back to the caller  
                AddFailedWebHookEntryToLog(hookRequest);
                return StatusCode(500, $"Webhook #{hookId} is in a error state");   //Error response
            }
            else
            {
                AddWebHookEntryToLog(hookRequest);

                // Send a separate notification to the SignalR based webhooks page.
                await SendToSignalR(hookId, hookRequest);
            }

            WebHookValidation.CheckIfEventGridSchemaValdationRequest(e =>
                {
                    //This lambda is called when a webhook validation request is sent back to the caller
                    AddCallbackEntryToLog(e);

                }, hookRequest, logger);

            WebHookValidation.CheckIfCloudEventValidationRequest(HttpContext, e =>
                {
                    //This lambda is called when a webhook validation request is sent back to the caller
                    AddCallbackEntryToLog(e);

                }, hookRequest, logger);

            return Ok($"Webhook #{hookId} is responding");          //OK response
        }
        catch (Exception exc)
        {
            AddExceptionEntryToLog(hookId, exc);
            return StatusCode(500);
        }
    }


    /// <summary>
    /// Send the webhook entry to the SignalR hubContext
    /// </summary>
    /// <param name="hookId"></param>
    /// <param name="entry"></param>
    /// <returns></returns>
    private async Task SendToSignalR(int hookId, WebHookLogEntry entry)
    {
        if (entry == null)
            return;

        //This is the destination message for the SignalR hub
        string signalRMessage = $"ReceiveMessage{hookId}";

        int hashcode = 0;

        //Base the color of the message on the hashcode of the subject or body
        if (!string.IsNullOrEmpty(entry.Subject))
        {
            hashcode = entry.Subject.GetHashCode() % 4;
        }
        else
        {
            if (!string.IsNullOrEmpty(entry.Body))
            {
                hashcode = entry.Body.GetHashCode() % 4;
            }
        }

        string color = hashcode switch
        {
            0 => "black",
            1 => "blue",
            2 => "green",
            3 => "red",
            _ => "yellow",
        };

        var content = "[M]";

        if (WebHookCounter++ % 15 == 0)
        {
            //Add a new line to ensure it wraps every 15 items
            content = "\r\n" + content;
        }


        await signalRhubContext.Clients.All.SendAsync(signalRMessage, color, content);
    }

    /// <summary>
    /// Add a standard webhook entry to the log and to the SignalR hubContext
    /// </summary>
    /// <param name="hookId"></param>
    /// <param name="entry"></param>
    private void AddWebHookEntryToLog(WebHookLogEntry entry)
    {
        webHookLog.AddToLog(entry);


    }

    /// <summary>
    /// Add a simulated "failed" webhook request to the log
    /// </summary>
    /// <param name="hookId"></param>
    /// <param name="entry"></param>
    private void AddFailedWebHookEntryToLog(WebHookLogEntry entry)
    {
        entry.Comment = $"{entry.Comment} (Returned a HTPP 500 Server Error response to the caller for #{entry.HookId})";
        webHookLog.AddToLog(entry);
    }

    /// <summary>
    /// Add an extra entry to the log that we have sent a validation request back to the caller
    /// </summary>
    /// <param name="hookId"></param>
    /// <param name="entry"></param>
    private void AddCallbackEntryToLog(WebHookLogEntry entry)
    {
        webHookLog.AddToLog(entry);
    }

    /// <summary>
    /// Add to the log that the hook request processing failed due to some internal exception/problem
    /// </summary>
    /// <param name="hookId"></param>
    /// <param name="exc"></param>
    private void AddExceptionEntryToLog(int hookId, Exception exc)
    {
        var entry = new WebHookLogEntry()
        {
            HookId = hookId,
            EntryTime = DateTime.UtcNow,
        };

        entry.Comment = entry.Comment + $"\r\n" + exc.Message;
        webHookLog.AddToLog(entry);
    }
}

// Source https://github.com/pm7y/AzureEventGridSimulator/blob/master/src/AzureEventGridSimulator/Domain/Services/SubscriptionValidationResponse.cs
public class SubscriptionValidationResponse
{
    [JsonProperty(PropertyName = "validationResponse", Required = Required.Always)]
    public Guid ValidationResponse { get; set; }
}