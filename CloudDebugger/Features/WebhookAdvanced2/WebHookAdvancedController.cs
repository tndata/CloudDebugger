/*CloudDebugger.Shared_code.WebHooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text;

namespace CloudDebugger.Features.WebhookAdvanced;

public class WebHookAdvancedController : Controller
{
    private readonly ILogger<WebHookAdvancedController> _logger;
    private readonly IHubContext<WebHookHub> hubContext;

    private static bool hookFailureEnabled = false;
    private static bool hideHeaders = false;
    private static bool hideBody = false;


    public WebHookAdvancedController(ILogger<WebHookAdvancedController> logger, IHubContext<WebHookHub> hubContext)
    {
        _logger = logger;
        this.hubContext = hubContext;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Hook()
    {
        ViewData["HookFailureEnabled"] = hookFailureEnabled;
        ViewData["HideHeaders"] = hideHeaders;
        ViewData["HideBody"] = hideBody;

        return View();
    }




    public IActionResult SetWebHookFailureMode(int id)
    {
        if (id == 0)
            hookFailureEnabled = false;
        else
            hookFailureEnabled = true;

        return Redirect("/WebHookAdvanced/Hook");
    }

    public IActionResult SetHideHeadersMode(int id)
    {
        if (id == 0)
            hideHeaders = false;
        else
            hideHeaders = true;

        return Redirect("/WebHookAdvanced/Hook");
    }

    public IActionResult SetHideBodyMode(int id)
    {
        if (id == 0)
            hideBody = false;
        else
            hideBody = true;

        return Redirect("/WebHookAdvanced/Hook");
    }



    /// <summary>
    /// This is the webhook endpoints that is called by services that are sending webhook requests.
    /// /WebhookAdvanced/Hook1
    /// /WebhookAdvanced/Hook2
    /// /WebhookAdvanced/Hook3
    /// /WebhookAdvanced/Hook4
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [HttpPost]
    [HttpOptions]
    public Task<IActionResult> Hook1()
    {
        return ProcessHook(1);
    }
    public Task<IActionResult> Hook2()
    {
        return ProcessHook(2);
    }
    public Task<IActionResult> Hook3()
    {
        return ProcessHook(3);
    }
    public Task<IActionResult> Hook4()
    {
        return ProcessHook(4);
    }

    private async Task<IActionResult> ProcessHook(int hookId)
    {
        IActionResult result;
        try
        {
            bool failed = false;

            var hookEntry = WebHookUtility.GetRequestDetails(Request, _logger);

            if (hookFailureEnabled)
            {
                // Simulate a webhook failure
                result = StatusCode(500);   //Error response
                hookEntry.Comment = hookEntry.Comment + $" (Return 500 hook #{hookId} error)";
                failed = true;
            }
            else
            {
                result = Ok("OK");          //OK response
            }
            HandleHook(hookId, failed, hookEntry);


            WebHookValidation.CheckIfEventGridSchemaValdationRequest(e =>
            {
                //Called when we want to add a notice to the log when the validation have sent a validation request back
                HandleHook(hookId, false, e);

            }, hookEntry);
            WebHookValidation.CheckIfCloudEventValidationRequest(HttpContext, e =>
            {
                //Called when we want to add a notice to the log when the validation have sent a validation request back
                HandleHook(hookId, false, e);

            }, hookEntry);
        }
        catch (Exception exc)
        {
            result = StatusCode(500);
            await TrySendToSignalR(hookId, exc.Message, "red");
        }

        return result;
    }

    private void HandleHook(int hookId, bool failed, WebHookLogEntry entry)
    {

        var color = "Black";
        if (failed)
            color = "Red";


        var SB = new StringBuilder();

        var time = entry.EntryTime.ToString("hh:mm:ss");

        SB.AppendLine($"\r\n{time} {entry.HttpMethod} {entry.Url}");
        //}
        //else
        //{
        //    //Too long line, do wrap it over multiple lines
        //    var chunks = entry.Url.AsEnumerable().Chunk(60).ToList();

        //    var prefix = $"\r\n{time} {entry.HttpMethod} ";
        //    foreach (var chunk in chunks)
        //    {
        //        SB.AppendLine($"{prefix}{new string(chunk)}");
        //        prefix = " - ";
        //    }

        if (string.IsNullOrEmpty(entry.Comment) == false)
            SB.AppendLine($"{entry.Comment}");


        if (hideHeaders == false)
        {
            foreach (var header in entry.RequestHeaders)
            {
                SB.AppendLine($"{header.Key}: {header.Value}");
                ////Too long line, do wrap it over multiple lines
                //var chunks = header.Value.AsEnumerable().Chunk(60).ToList();

                //var prefix = $"{header.Key}: ";
                //foreach (var chunk in chunks)
                //{
                //    SB.AppendLine($"{prefix}{new string(chunk)}");
                //    prefix = " - ";
                //}
            }
        }

        if (hideBody == false && entry.Body != null && entry.Body.Length > 0)
        {
            SB.AppendLine("");
            SB.AppendLine($"{entry.Body}");
        }



        TrySendToSignalR(hookId, SB.ToString(), color).Wait();
    }





    private async Task TrySendToSignalR(int hookId, string message, string color)
    {
        string signalRHookMessage = $"ReceiveMessage{hookId}";
        await hubContext.Clients.All.SendAsync(signalRHookMessage, color, message);
    }
}



*/