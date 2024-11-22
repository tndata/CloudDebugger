using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.Configuration;

/// <summary>
/// This tool does the following:
/// 
/// * Lists all the ASP.NET Configuration data
/// 
/// * Documentation
///   https://github.com/tndata/CloudDebugger/wiki/Configuration
/// </summary>
public class ConfigurationController : Controller
{
    private readonly IConfiguration configuration;

    public ConfigurationController(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public IActionResult Index()
    {
        var model = new AppConfigurationModel();

        var root = (IConfigurationRoot)configuration;

        model.DebugView = root.GetDebugView();

        model.Config = configuration.AsEnumerable().OrderBy(c => c.Key).ToList();

        model.ConfigProviders = root.Providers
             .Select(provider =>
             {
                 var details = new ConfigurationProviderDetails
                 {
                     Name = provider.ToString(),

                     Prefix = (provider as IConfigurationSource)?.ToString(), // Example of getting prefix, adjust as needed

                     ChildKeys = provider.GetChildKeys([], null).ToList()
                 };

                 // Get values for each key
                 foreach (var key in details.ChildKeys)
                 {
                     if (provider.TryGet(key, out var value))
                     {
                         details.Values[key] = value;
                     }
                 }

                 return details;
             })
             .ToList();

        return View(model);
    }
}
