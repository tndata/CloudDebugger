using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.WebHooks;

/// <summary>
/// 
/// </summary>
public class WebHooksController : Controller
{
    private readonly ILogger<WebHooksController> _logger;
    private readonly IWebHookLog webHookLog;

    public WebHooksController(ILogger<WebHooksController> logger, IWebHookLog webHookLog)
    {
        _logger = logger;
        this.webHookLog = webHookLog;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Overview()
    {
        return View();
    }

    public IActionResult WebHooksLog(int hookId)
    {
        if (!ModelState.IsValid || hookId < 1 || hookId > 4)
            return BadRequest(ModelState);

        ViewData["HookId"] = hookId;

        return View();
    }

    public IActionResult SetWebHookFailureMode(int hookId, int id)
    {
        _logger.LogInformation("SetWebHookFailureMode hookId={HookId} id={Id}", hookId, id);

        if (!ModelState.IsValid || hookId < 1 || hookId > 4)
            return BadRequest(ModelState);

        if (id == 0)
            WebHookSettings.WebHookFailureEnabled = false;
        else
            WebHookSettings.WebHookFailureEnabled = true;

        return Redirect($"/WebHooks/WebHooksLog?hookId={hookId}");
    }

    public IActionResult SetHideHeadersMode(int hookId, int id)
    {
        _logger.LogInformation("SetHideHeadersMode hookId={HookId} id={Id}", hookId, id);

        if (!ModelState.IsValid || hookId < 1 || hookId > 4)
            return BadRequest(ModelState);


        if (id == 0)
            WebHookSettings.HideHeaders = false;
        else
            WebHookSettings.HideHeaders = true;

        return Redirect($"/WebHooks/WebHooksLog?hookId={hookId}");
    }

    public IActionResult SetHideBodyMode(int hookId, int id)
    {
        _logger.LogInformation("SetHideBodyMode hookId={HookId} id={Id}", hookId, id);

        if (!ModelState.IsValid || hookId < 1 || hookId > 4)
            return BadRequest(ModelState);

        if (id == 0)
            WebHookSettings.HideBody = false;
        else
            WebHookSettings.HideBody = true;

        return Redirect($"/WebHooks/WebHooksLog?hookId={hookId}");
    }

    public IActionResult ClearWebHookLog(int hookId)
    {
        _logger.LogInformation("ClearWebHookLog hookId={HookId}", hookId);

        if (!ModelState.IsValid || hookId < 1 || hookId > 4)
            return BadRequest(ModelState);

        webHookLog.ClearLog();

        return Redirect($"/WebHooks/WebHooksLog?hookId={hookId}");
    }
}

