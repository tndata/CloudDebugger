using Azure.MyIdentity;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.CredentialsEventLog;

/// <summary>
/// This controller is used to view the internal custom Eventlog from the custom MyAzureIdentity library log class.
/// </summary>
public class CredentialsEventLogController : Controller
{
    public CredentialsEventLogController()
    {
    }

    public IActionResult ViewEventLog()
    {
        // Get the internal custom Eventlog from the custom MyAzureIdentity library log class
        var model = new ViewLogModel()
        {
            Log = MyAzureIdentityLog.Log
        };

        return View(model);
    }

    public IActionResult ClearEventLog()
    {
        MyAzureIdentityLog.ClearLog();
        return RedirectToAction("ViewEventLog");
    }
}
