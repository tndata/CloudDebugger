using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;


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

    public IActionResult Index(int hookId)
    {
        if (hookId != 1 && hookId != 2)
            hookId = 1; // Default to hook #1   

        ViewData["HookFailureEnabled"] = hookFailureEnabled;
        ViewData["HideHeaders"] = hideHeaders;
        ViewData["HideBody"] = hideBody;
        ViewData["HookId"] = hookId;

        if (hookId == 1)
            return View("Index", webHook1);
        else
            return View("Index", webHook2);
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
            return Redirect("/WebHook/Index?hookId=1");
        else
            return Redirect("/WebHook/Index?hookId=2");
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
            return Redirect("/WebHook/Index?hookId=1");
        else
            return Redirect("/WebHook/Index?hookId=2");
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
            return Redirect("/WebHook/Index?hookId=1");
        else
            return Redirect("/WebHook/Index?hookId=2");
    }

    public IActionResult ClearWebHookLog(int hookId)
    {
        if (hookId != 1 && hookId != 2)
            hookId = 1; // Default to hook #1   

        if (hookId == 1)
        {
            webHook1.ClearLog();
            return Redirect("/WebHook/Index?hookId=1");
        }
        else
        {
            webHook2.ClearLog();
            return Redirect("/WebHook/Index?hookId=2");
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
            var logEntry = GetRequestDetails();

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

            WebHookValidation.CheckIfEventGridSchemaValdationRequest(webHook, logEntry);
            WebHookValidation.CheckIfCloudEventValidationRequest(HttpContext, webHook, logEntry);

            return result;
        }
    }


    private WebHookLogEntry GetRequestDetails()
    {
        var logEntry = new WebHookLogEntry();

        logEntry.EntryTime = DateTime.Now;
        logEntry.Comment = "";

        if (Request.Headers.ContainsKey("Content-Type"))
        {
            logEntry.ContentType = Request.Headers["Content-Type"].FirstOrDefault() ?? "[Unknown]";
        }

        //Get request body 
        logEntry.Body = "";
        if (Request.ContentLength > 0)
        {
            using (var reader = new StreamReader(Request.Body))
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
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in Request.Headers.OrderBy(h => h.Key))
        {
            logEntry.Headers.Add(header.Key, header.Value.ToString() ?? "");
        }

        logEntry.HttpMethod = Request.Method;
        logEntry.Url = Request.Path.Value ?? "";
        return logEntry;
    }
}

