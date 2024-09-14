using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Caching;

public class CachingController : Controller
{
    public CachingController()
    {
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult GetTimeNoCacheControl()
    {
        return Ok(DateTime.UtcNow.ToString());
    }

    public IActionResult GetTimeWithCacheControl()
    {
        Response.Headers.CacheControl = "public, max-age=5";
        return Ok("Cache max for 5 seconds: " + DateTime.UtcNow.ToString());
    }

    public IActionResult GetTimePrivateCacheControl()
    {
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate, proxy-revalidate, private  ";

        // Optionally, set other related headers
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        return Ok("Should not be cached: " + DateTime.UtcNow.ToString());
    }
}
