using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.BlobStorage;

public class BlobStorageController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
