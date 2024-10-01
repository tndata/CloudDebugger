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
/// </summary>
[EnableCors("MyCorsPolicy_wildcard")]
[Route("/")]
[ApiController]
public class WebHookApiController : ControllerBase
{
    private readonly ILogger<WebHookApiController> logger;
    private readonly IWebHookLog webHookLog;
    private readonly IHubContext<WebHookHub> hubContext;

    public WebHookApiController(ILogger<WebHookApiController> logger,
                                IWebHookLog webHookLog,
                                IHubContext<WebHookHub> hubContext)
    {
        this.logger = logger;
        this.webHookLog = webHookLog;
        this.hubContext = hubContext;
    }

    [HttpPost("hook1")]
    [HttpOptions("hook1")]
    public Task<IActionResult> Hook1()
    {
        return ProcessHook(1);
    }

    [HttpPost("hook2")]
    [HttpOptions("hook2")]
    public Task<IActionResult> Hook2()
    {
        return ProcessHook(2);
    }

    [HttpPost("hook3")]
    [HttpOptions("hook3")]
    public Task<IActionResult> Hook3()
    {
        return ProcessHook(3);
    }

    [HttpPost("hook4")]
    [HttpOptions("hook4")]
    public Task<IActionResult> Hook4()
    {
        return ProcessHook(4);
    }

    private async Task<IActionResult> ProcessHook(int hookId)
    {
        IActionResult result;
        try
        {
            var hookRequest = await WebHookUtility.GetRequestDetails(Request, logger);

            if (WebHookSettings.WebHookFailureEnabled)
            {
                // Simulate a webhook failure by returning a HTTP 500 Server Errror back to the caller  
                AddFailedWebHookEntryToLog(hookId, hookRequest);
                result = StatusCode(500);   //Error response
            }
            else
            {
                AddWebHookEntryToLog(hookId, hookRequest);
                await SendToSignalR(hookId, hookRequest);
                result = Ok("OK");          //OK response
            }

            WebHookValidation.CheckIfEventGridSchemaValdationRequest(e =>
                {
                    //This lambda is called when a webhook validation request is sent back to the caller
                    AddCallbackEntryToLog(hookId, e);

                }, hookRequest, logger);
            WebHookValidation.CheckIfCloudEventValidationRequest(HttpContext, e =>
                {
                    //This lambda is called when a webhook validation request is sent back to the caller
                    AddCallbackEntryToLog(hookId, e);

                }, hookRequest, logger);

        }
        catch (Exception exc)
        {
            result = StatusCode(500);
            AddExceptionEntryToLog(hookId, exc);
        }

        return result;
    }

    private async Task SendToSignalR(int hookId, WebHookLogEntry entry)
    {
        string signalRMessage = $"ReceiveMessage{hookId}";
        string color = "";

        switch (entry?.Body?.GetHashCode() % 4)
        {
            case 0:
                color = "red";
                break;
            case 1:
                color = "blue";
                break;
            case 2:
                color = "green";
                break;
            case 3:
                color = "black";
                break;
        }

        var content = "[M]";

        await hubContext.Clients.All.SendAsync(signalRMessage, color, content);
    }

    /// <summary>
    /// Add a standard webhook entry to the log and to the SignalR hubContext
    /// </summary>
    /// <param name="hookId"></param>
    /// <param name="entry"></param>
    private void AddWebHookEntryToLog(int hookId, WebHookLogEntry entry)
    {
        webHookLog.AddToLog(hookId, entry);


    }

    /// <summary>
    /// Add a simulated "failed" webhook request to the log
    /// </summary>
    /// <param name="hookId"></param>
    /// <param name="entry"></param>
    private void AddFailedWebHookEntryToLog(int hookId, WebHookLogEntry entry)
    {
        entry.Comment = $"{entry.Comment} (Returned a HTPP 500 Server Error response to the caller for #{hookId})";
        webHookLog.AddToLog(hookId, entry);
    }

    /// <summary>
    /// Add an extra entry to the log that we have sent a validation request back to the caller
    /// </summary>
    /// <param name="hookId"></param>
    /// <param name="entry"></param>
    private void AddCallbackEntryToLog(int hookId, WebHookLogEntry entry)
    {
        webHookLog.AddToLog(hookId, entry);
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
        };

        entry.Comment = entry.Comment + $"\r\n" + exc.Message;
        webHookLog.AddToLog(hookId, entry);
    }
}
