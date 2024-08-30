using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.CallingApis;

public class CallingApisController : Controller
{
    private readonly ILogger<CallingApisController> _logger;

    public CallingApisController(ILogger<CallingApisController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
    public IActionResult SlowApi()
    {
        var url = "https://httpbin.org/delay/5";

        var result = MakeCallToApi(url);

        ViewData["Message"] = "Request sent, result = " + result;

        return View("Index");
    }

    public IActionResult ErrorApi()
    {
        var url = "https://httpbin.org/status/500";

        var result = MakeCallToApi(url);

        ViewData["Message"] = "Request sent, result = " + result;

        return View("Index");
    }

    private static string MakeCallToApi(string url)
    {
        try
        {
            var client = new HttpClient();
            var result = client.GetStringAsync(url).Result;

            //TODO: Handle result?
            return "OK";
        }
        catch (Exception exc)
        {
            return "Error: " + exc.Message;
        }
    }

}
