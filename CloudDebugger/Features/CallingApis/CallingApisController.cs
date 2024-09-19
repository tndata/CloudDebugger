using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.CallingApis;

/// <summary>
/// Calling external APIs
/// 
/// </summary>
public class CallingApisController : Controller
{
    private readonly Uri JSONApiUrl = new("https://httpbin.org/json");
    private readonly Uri slowApiUrl = new("https://httpbin.org/delay/5");
    private readonly Uri error500ApiUrl = new("https://httpbin.org/status/500");

    private readonly ILogger<CallingApisController> _logger;

    public CallingApisController(ILogger<CallingApisController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> JsonApi()
    {
        _logger.LogInformation("JSON API endpoint was called");

        var result = await MakeCallToExternalApi(JSONApiUrl);

        ViewData["Message"] = "Request sent, result = " + result;

        return View("Index");
    }

    public async Task<IActionResult> SlowApi()
    {
        _logger.LogInformation("Slow API endpoint was called");

        var result = await MakeCallToExternalApi(slowApiUrl);

        ViewData["Message"] = "Request sent, result = " + result;

        return View("Index");
    }

    public async Task<IActionResult> ErrorApi()
    {
        _logger.LogInformation("Error API endpoint was called");

        var result = await MakeCallToExternalApi(error500ApiUrl);

        ViewData["Message"] = "Request sent, result = " + result;

        return View("Index");
    }

    private static async Task<string> MakeCallToExternalApi(Uri url)
    {
        try
        {
            using var client = new HttpClient();

            // We ignore the result from this call.
            var response = await client.GetStringAsync(url);

            return response;
        }
        catch (Exception exc)
        {
            return "Error: " + exc.Message;
        }
    }

}
