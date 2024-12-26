using CloudDebugger.SharedCode.AzureSdkEventLogger;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.AzureSdkEventLogViewer;

/// <summary>
/// This controller displays the internal events collected from the Azure SDK.
/// </summary>
public class AzureSdkEventLogViewerController : Controller
{
    public AzureSdkEventLogViewerController()
    {
    }

    public IActionResult ViewEventLog()
    {
        var model = new ViewLogModel()
        {
            Log = AzureEventLogger.LogEntries
        };

        return View(model);
    }

    public IActionResult ClearEventLog()
    {
        AzureEventLogger.ClearLog();
        return RedirectToAction("ViewEventLog");
    }
}
