using CloudDebugger.Shared_code.WebHooks;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.WebHook;

public class WebHookController : Controller
{
    private readonly ILogger<WebHookController> _logger;
    private static bool hookFailureEnabled = false;
    private static bool hideHeaders = false;
    private static bool hideBody = false;

    private static WebHookLog webHook1 = new();
    private static WebHookLog webHook2 = new();

    public WebHookController(ILogger<WebHookController> logger)
    {
        _logger = logger;
    }


    public IActionResult Index()
    {
        return View();
    }


    public IActionResult Hook(int hookId)
    {
        if (hookId != 1 && hookId != 2)
            hookId = 1; // Default to hook #1   

        ViewData["HookFailureEnabled"] = hookFailureEnabled;
        ViewData["HideHeaders"] = hideHeaders;
        ViewData["HideBody"] = hideBody;
        ViewData["HookId"] = hookId;

        if (hookId == 1)
            return View("Hook", webHook1);
        else
            return View("Hook", webHook2);
    }


    public IActionResult SetWebHookFailureMode(int hookId, int id)
    {
        if (hookId != 1 && hookId != 2)
            hookId = 1; // Default to hook #1   

        if (id == 0)
            hookFailureEnabled = false;
        else
            hookFailureEnabled = true;

        if (hookId == 1)
            return Redirect("/WebHook/Hook?hookId=1");
        else
            return Redirect("/WebHook/Hook?hookId=2");
    }

    public IActionResult SetHideHeadersMode(int hookId, int id)
    {
        if (hookId != 1 && hookId != 2)
            hookId = 1; // Default to hook #1   

        if (id == 0)
            hideHeaders = false;
        else
            hideHeaders = true;

        if (hookId == 1)
            return Redirect("/WebHook/Hook?hookId=1");
        else
            return Redirect("/WebHook/Hook?hookId=2");
    }

    public IActionResult SetHideBodyMode(int hookId, int id)
    {
        if (hookId != 1 && hookId != 2)
            hookId = 1; // Default to hook #1   

        if (id == 0)
            hideBody = false;
        else
            hideBody = true;

        if (hookId == 1)
            return Redirect("/WebHook/Hook?hookId=1");
        else
            return Redirect("/WebHook/Hook?hookId=2");
    }

    public IActionResult ClearWebHookLog(int hookId)
    {
        if (hookId != 1 && hookId != 2)
            hookId = 1; // Default to hook #1   

        if (hookId == 1)
        {
            webHook1.ClearLog();
            return Redirect("/WebHook/Hook?hookId=1");
        }
        else
        {
            webHook2.ClearLog();
            return Redirect("/WebHook/Hook?hookId=2");
        }
    }


    /// <summary>
    /// This is the webhook #1 endpoint that is called by services that are sending webhook requests.
    /// /WebHook/Hook2
    /// /// </summary>
    /// <returns></returns>
    public IActionResult Hook1()
    {
        return ProcessHook(webHook1);
    }


    /// <summary>
    /// This is the webhook #2 endpoint that is called by services that are sending webhook requests.
    /// /WebHook/Hook2
    /// </summary>
    /// <returns></returns>
    public IActionResult Hook2()
    {
        return ProcessHook(webHook2);
    }


    private IActionResult ProcessHook(WebHookLog webHook)
    {
        lock (this)
        {
            var logEntry = WebHookUtility.GetRequestDetails(Request, _logger);

            IActionResult result;

            if (hookFailureEnabled)
            {
                // Simulate a failure
                logEntry.Comment = logEntry.Comment + " (Return 500 hook #1 error)";
                result = StatusCode(500);
            }
            else
            {
                result = Ok("OK");
            }

            webHook.AddToLog(logEntry);

            WebHookValidation.CheckIfEventGridSchemaValdationRequest(e =>
            {
                //Called when we want to add a notice to the log when the validation have sent a validation request back
                webHook.AddToLog(e);

            }, logEntry);
            WebHookValidation.CheckIfCloudEventValidationRequest(HttpContext, e =>
            {
                //Called when we want to add a notice to the log when the validation have sent a validation request back
                webHook.AddToLog(e);

            }, logEntry);

            //WebHookValidation.CheckIfEventGridSchemaValdationRequest(webHook, logEntry);
            //WebHookValidation.CheckIfCloudEventValidationRequest(HttpContext, webHook, logEntry);

            return result;
        }
    }

}

