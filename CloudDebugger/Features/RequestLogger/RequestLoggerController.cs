using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.MyHttpLogging;

namespace CloudDebugger.Features.RequestLogger;

/// <summary>
/// Display the request/response log as captured by the custom MyHttpLibrary request middleware.
/// </summary>
public class RequestLoggerController : Controller
{
    public RequestLoggerController()
    {
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult CurrentRequest()
    {
        return View();
    }

    public IActionResult RequestList()
    {
        return View();
    }

    public IActionResult ClearRequestList()
    {
        RequestLog.ClearLog();

        return RedirectToAction("RequestList");
    }

    public IActionResult RequestDetails(int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var logEntry = RequestLog.LookupLogEntry(id);

        return View(logEntry);
    }
}
