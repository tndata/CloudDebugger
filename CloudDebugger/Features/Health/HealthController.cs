using CloudDebugger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Health;

public class HealthController : Controller
{
    private readonly ILogger<HealthController> _logger;

    private static CustomHealthStatusEnum CustomHealthEndpointStatus { get; set; } = CustomHealthStatusEnum.Healthy;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new HealthModel
        {
            CurrentServiceHealth = DebuggerSettings.ServiceHealth,
            CurrentCustomHealth = CustomHealthEndpointStatus
        };

        return View(model);
    }

    /// <summary>
    /// The ServiceHealth controls the health of this application service.This is the overall health as exposed by the /healthz and /health endpoints.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult SetServiceHealthMode(HealthModel model, string mode)
    {
        if (ModelState.IsValid)
        {
            switch (mode)
            {
                case "Healthy":
                    DebuggerSettings.ServiceHealth = HealthStatusEnum.Healthy;

                    break;
                case "Degraded":
                    DebuggerSettings.ServiceHealth = HealthStatusEnum.Degraded;
                    break;
                case "Unhealthy":
                    DebuggerSettings.ServiceHealth = HealthStatusEnum.Unhealthy;
                    break;
                default:
                    //No change
                    break;
            }

            _logger.LogInformation("Set ServiceHealthMode action called, mode={Mode}", DebuggerSettings.ServiceHealth);

            model ??= new HealthModel();

            model.CurrentServiceHealth = DebuggerSettings.ServiceHealth;
            model.CurrentCustomHealth = CustomHealthEndpointStatus;
        }

        return View("index", model);
    }


    /// <summary>
    /// The custom health controls the health of the custom local endpoint /health/CustomEndpoint endpoint.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult SetCustomHealthMode(HealthModel model, string mode)
    {
        if (ModelState.IsValid)
        {
            switch (mode)
            {
                case "healthy":
                    CustomHealthEndpointStatus = CustomHealthStatusEnum.Healthy;
                    break;
                case "delayed10s":
                    CustomHealthEndpointStatus = CustomHealthStatusEnum.Delayed10s;
                    break;
                case "delayed60s":
                    CustomHealthEndpointStatus = CustomHealthStatusEnum.Delayed60s;
                    break;
                case "error":
                    CustomHealthEndpointStatus = CustomHealthStatusEnum.Error;
                    break;
                default:
                    //No change
                    break;
            }

            _logger.LogInformation("Set CustomHealthMode action called, mode={Mode}", CustomHealthEndpointStatus);

            model ??= new HealthModel();

            model.CurrentServiceHealth = DebuggerSettings.ServiceHealth;
            model.CurrentCustomHealth = CustomHealthEndpointStatus;
        }

        return View("index", model);
    }


    public IActionResult CustomEndpoint()
    {
        _logger.LogInformation("CustomEndpoint action called");

        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        switch (CustomHealthEndpointStatus)
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
