using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.ConnectionStrings;

/// <summary>
/// This tool does the following:
/// 
/// * Lists all the connection strings found by ASP.NET Core
/// 
/// </summary>
public class ConnectionStringsController : Controller
{
    private readonly IConfiguration configuration;

    public ConnectionStringsController(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public IActionResult Index()
    {
        var model = new ConnectionStringsModel();

        var connectionStringsSection = configuration.GetSection("ConnectionStrings");
        foreach (var connectionString in connectionStringsSection.AsEnumerable())
        {
            if (!string.IsNullOrEmpty(connectionString.Value))
            {
                model.ConnectionStrings.Add(connectionString.Key, connectionString.Value);
            }
        }

        return View(model);
    }
}
