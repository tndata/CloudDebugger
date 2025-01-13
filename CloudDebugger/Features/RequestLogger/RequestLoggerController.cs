using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.MyHttpLogging;

namespace CloudDebugger.Features.RequestLogger;

/// <summary>
/// This tools provides the following tools
/// 
/// * Request Logger
///   Display the request/response log as captured by the custom MyHttpLibrary request middleware.
/// * Current request viewer
///   This tool provides detailed insights into the current HTTP(s) request.
///
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

    public IActionResult RequestDetails([FromRoute] int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var logEntry = RequestLog.LookupLogEntry(id);

        return View(logEntry);
    }
}
