using CloudDebugger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Health;

public class HealthController : Controller
{
    private readonly ILogger<HealthController> _logger;

    private static CustomHealthStatusEnum customHealthEndpointStatus { get; set; } = CustomHealthStatusEnum.Healthy;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new HealthModel
        {
            CurrentServiceHealth = Settings.ServiceHealth,
            CurrentCustomHealth = customHealthEndpointStatus
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult SetServiceHealthMode(HealthModel model, string mode)
    {
        switch (mode)
        {
            case "Healthy":
                Settings.ServiceHealth = HealthStatusEnum.Healthy;
                break;
            case "Degraded":
                Settings.ServiceHealth = HealthStatusEnum.Degraded;
                break;
            case "Unhealthy":
                Settings.ServiceHealth = HealthStatusEnum.Unhealthy;
                break;
            default:
                //No change
                break;
        }

        if (model == null)
            model = new HealthModel();

        model.CurrentServiceHealth = Settings.ServiceHealth;
        model.CurrentCustomHealth = customHealthEndpointStatus;

        return View("index", model);
    }


    [HttpPost]
    public IActionResult SetCustomHealthMode(HealthModel model, string mode)
    {
        switch (mode)
        {
            case "healthy":
                customHealthEndpointStatus = CustomHealthStatusEnum.Healthy;
                break;
            case "delayed10s":
                customHealthEndpointStatus = CustomHealthStatusEnum.Delayed10s;
                break;
            case "delayed60s":
                customHealthEndpointStatus = CustomHealthStatusEnum.Delayed60s;
                break;
            case "error":
                customHealthEndpointStatus = CustomHealthStatusEnum.Error;
                break;
            default:
                //No change
                break;
        }

        if (model == null)
            model = new HealthModel();

        model.CurrentServiceHealth = Settings.ServiceHealth;
        model.CurrentCustomHealth = customHealthEndpointStatus;

        return View("index", model);
    }


    public IActionResult CustomEndpoint()
    {
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        switch (customHealthEndpointStatus)
        {
            case CustomHealthStatusEnum.Healthy:
                return Ok("I am healthy, current time=" + time);
            case CustomHealthStatusEnum.Delayed10s:
                System.Threading.Thread.Sleep(10000);
                return Ok("I am healthy, but 10 sec slow, current time=" + time);
            case CustomHealthStatusEnum.Delayed60s:
                System.Threading.Thread.Sleep(60000);
                return Ok("I am healthy, but 60 sec slow, current time=" + time);
            case CustomHealthStatusEnum.Error:
                return StatusCode(500, "I am not healthy, current time=" + time);
            default:
                return Ok("Unknown status");
        }
    }
}
