using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Api;

/// <summary>
/// Controller for the REST API HTML HTML page
/// </summary>
public class ApiController : Controller
{
    public ApiController()
    {
    }

    public IActionResult Index()
    {
        return View();
    }
}
