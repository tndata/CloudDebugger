using Azure.MyIdentity;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.CredentialsEventLog;

/// <summary>
/// This controller is used to view the internal custom Eventlog from the custom MyAzureIdentity library.
/// </summary>
public class CredentialsEventLogController : Controller
{
    public CredentialsEventLogController()
    {
    }

    public IActionResult ViewEventLog()
    {
        var model = new ViewLogModel()
        {
            Log = MyAzureIdentityLog.LogEntries
        };

        return View(model);
    }

    public IActionResult ClearEventLog()
    {
        MyAzureIdentityLog.ClearLog();
        return RedirectToAction("ViewEventLog");
    }
}
