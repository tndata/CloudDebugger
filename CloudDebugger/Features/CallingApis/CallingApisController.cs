using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.CallingApis;

/// <summary>
/// Calling external APIs
/// 
/// TODO: DOCUMENT!
/// </summary>
public class CallingApisController : Controller
{
    private const string slowApiUrl = "https://httpbin.org/delay/5";
    private const string error500ApiUrl = "\"https://httpbin.org/status/500";

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
        _logger.LogInformation("SlowApi endpoint was called");

        var result = MakeCallToExternalApi(slowApiUrl);

        ViewData["Message"] = "Request sent, result = " + result;

        return View("Index");
    }

    public IActionResult ErrorApi()
    {
        //TODO: REVIEW!
        _logger.LogInformation("ErrorApi endpoint was called");

        var url = "https://httpbin.org/status/500";

        var result = MakeCallToExternalApi(url);

        ViewData["Message"] = "Request sent, result = " + result;

        return View("Index");
    }

    private static string MakeCallToExternalApi(string url)
    {
        try
        {
            var client = new HttpClient();
            var result = client.GetStringAsync(error500ApiUrl).Result;

            //TODO: Handle result?
            return "OK";
        }
        catch (Exception exc)
        {
            return "Error: " + exc.Message;
        }
    }

}
