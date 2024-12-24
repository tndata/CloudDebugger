using CloudDebugger.SharedCode.AzureSDKEventLogger;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.AzureSDKEventLogViewer;

/// <summary>
/// This controller displays the internal events collected from the Azure SDK.
/// </summary>
public class AzureSDKEventLogViewerController : Controller
{
    public AzureSDKEventLogViewerController()
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
