using CloudDebugger.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.MyHttpLogging;
using Microsoft.Extensions.Options;

namespace CloudDebugger.Features.RequestLogger;

/// <summary>
/// This tools provides the following tools
/// 
/// * Request List
///   Display the request/response log as captured by the custom MyHttpLibrary request middleware.
/// * Current request viewer
///   This tool provides detailed insights into the current HTTP(s) request.
/// * Forwarded Headers Configuration
///   Displays the current configuration of the Forwarded Headers middleware.
///
/// </summary>
public class RequestLoggerController : Controller
{
    private readonly IOptions<ForwardedHeadersOptions> forwardedHeadersOptions;

    public RequestLoggerController(IOptions<ForwardedHeadersOptions> forwardedHeadersOptions)
    {
        this.forwardedHeadersOptions = forwardedHeadersOptions;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult CurrentRequest()
    {
        var model = new CurrentRequestModel
        {
            // Get details about the incoming request before it is modified by the middlewares
            // These values are captured by a custom Middleware
            RawRequest = HttpContext.Items["RawRequestDetails"] as RawRequestDetails,

            FinalDisplayUrl = HttpContext.Request.GetDisplayUrl()
        };

        return View(model);
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

    /// <summary>
    /// Displays the current configuration of the Forwarded Headers middleware in an ASP.NET Core
    ///
    /// In this application, the ASPNETCORE_FORWARDEDHEADERS_ENABLED environment variable has no function. 
    /// his is because the middleware is explicitly enabled using app.UseForwardedHeaders() in the application code.
    /// </summary>
    /// <returns></returns>
    public IActionResult ForwardedHeaders()
    {
        return View(forwardedHeadersOptions.Value);
    }
}
