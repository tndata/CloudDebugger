using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.HomePage
{
    public class CachingController : Controller
    {
        private readonly ILogger<CachingController> _logger;


        public CachingController(ILogger<CachingController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GetTimeNoCacheControl()
        {
            return Ok(DateTime.Now.ToString());
        }

        public IActionResult GetTimeWithCacheControl()
        {
            Response.Headers["Cache-Control"] = "public, max-age=5";
            return Ok("Cache max for 5 seconds: " + DateTime.Now.ToString());
        }

        public IActionResult GetTimePrivateCacheControl()
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0, must-revalidate, proxy-revalidate, private  ";

            // Optionally, set other related headers
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return Ok("Should not be cached: " + DateTime.Now.ToString());
        }
    }
}
