using CloudDebugger.SharedCode.WebHooks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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
/// * Accept HTTP POST And OPTIONS requests
/// * Get details about the request and then add it to the log
/// * Optionally, send a callback request back to the caller for validation purposes
/// * Supports Cloud events and Event Grid schema validation
/// 
/// 
/// Options:
/// * WebHookFailureEnable
/// TODO:!!!
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

    [HttpGet("hook{id:int:min(1):max(4)}")]
    public Task<IActionResult> HookGet(int id)
    {
        return Task.FromResult<IActionResult>(Ok($"Webhook #{id} is responding, but only supports HTTP POST and OPTIONS requests"));
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
    [HttpPost("hook{id:int:min(1):max(4)}")]
    [HttpOptions("hook{id:int:min(1):max(4)}")]
    public Task<IActionResult> Hook(int id)
    {
        return ProcessHook(id);
    }



    private async Task<IActionResult> ProcessHook(int hookId)
    {
        IActionResult result;
        try
        {
            var hookRequest = await WebHookUtility.GetRequestDetails(hookId, Request, logger);

            if (WebHookSettings.WebHookFailureEnabled)
            {
                // Simulate a webhook failure by returning a HTTP 500 Server Errror back to the caller  
                AddFailedWebHookEntryToLog(hookRequest);
                result = StatusCode(500);   //Error response
            }
            else
            {
                AddWebHookEntryToLog(hookRequest);
                await SendToSignalR(hookId, hookRequest);
                result = Ok("OK");          //OK response
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

        }
        catch (Exception exc)
        {
            result = StatusCode(500);
            AddExceptionEntryToLog(hookId, exc);
        }

        return result;
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

        string color = "black";
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

        switch (hashcode)
        {
            case 0:
                color = "black";
                break;
            case 1:
                color = "blue";
                break;
            case 2:
                color = "green";
                break;
            case 3:
                color = "red";
                break;
        }

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
