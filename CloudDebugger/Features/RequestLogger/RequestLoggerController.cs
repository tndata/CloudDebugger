using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.MyHttpLogging;

namespace CloudDebugger.Features.RequestLogger
{
    /// <summary>
    /// Display the request/response log 
    /// </summary>
    public class RequestLoggerController : Controller
    {
        private readonly ILogger<RequestLoggerController> _logger;

        public RequestLoggerController(ILogger<RequestLoggerController> logger)
        {
            _logger = logger;
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
            var logEntry = RequestLog.LookupLogEntry(id);

            return View(logEntry);
        }
    }
}
