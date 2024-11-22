using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.ConnectionStrings;

/// <summary>
/// This tool does the following:
/// 
/// * Lists all the connection strings found by ASP.NET Core
/// 
/// * Documentatioin
///   https://github.com/tndata/CloudDebugger/wiki/ConnectionStrings 
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
                var value = connectionString.Value;

                if (string.IsNullOrEmpty(value))
                    value = "[Empty]";

                model.ConnectionStrings.Add(connectionString.Key, value);
            }
        }

        return View(model);
    }
}
