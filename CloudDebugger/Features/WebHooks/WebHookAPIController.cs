using CloudDebugger.Shared_code.WebHooks;
using Microsoft.AspNetCore.Mvc;

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
[Route("api/[controller]")]
[ApiController]
public class WebHookAPIController : ControllerBase
{
    private readonly ILogger<WebHookAPIController> logger;
    private readonly IWebHookLog webHookLog;

    public WebHookAPIController(ILogger<WebHookAPIController> logger, IWebHookLog webHookLog)
    {
        this.logger = logger;
        this.webHookLog = webHookLog;
    }

    [Route("/hook1")]
    public Task<IActionResult> Hook1()
    {
        return ProcessHook(1);
    }

    [Route("/hook2")]
    public Task<IActionResult> Hook2()
    {
        return ProcessHook(2);
    }

    [Route("/hook3")]
    public Task<IActionResult> Hook3()
    {
        return ProcessHook(3);
    }

    [Route("/hook4")]
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
                result = Ok("OK");          //OK response
            }

            WebHookValidation.CheckIfEventGridSchemaValdationRequest(e =>
                {
                    //This lambda is called when a webhook validation request is sent back to the caller
                    AddCallbackEntryToLog(hookId, e);

                }, hookRequest);
            WebHookValidation.CheckIfCloudEventValidationRequest(HttpContext, e =>
                {
                    //This lambda is called when a webhook validation request is sent back to the caller
                    AddCallbackEntryToLog(hookId, e);

                }, hookRequest);

        }
        catch (Exception exc)
        {
            result = StatusCode(500);
            AddExceptionEntryToLog(hookId, exc);
        }

        return result;
    }


    /// <summary>
    /// Add a standard webhook entry to the log
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
        entry.Comment = entry.Comment + $" (Returned a HTPP 500 Server Error response to the caller for #{hookId})";
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
