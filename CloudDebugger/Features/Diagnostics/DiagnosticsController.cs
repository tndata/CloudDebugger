using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Diagnostics
{
    public class DiagnosticsController : Controller
    {
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(ILogger<DiagnosticsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult EnvironmentVariables()
        {
            return View();
        }

        public IActionResult Network()
        {
            //Inspiration https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.networkinterface?view=net-8.0&redirectedfrom=MSDN

            // More code here https://github.com/dotnet/dotnet-api-docs/blob/a4cef208decae4c2863337173050b2805ec8f706/snippets/csharp/System.Net.NetworkInformation/IcmpV4Statistics/Overview/netinfo.cs#L316
            return View();
        }
    }
}
