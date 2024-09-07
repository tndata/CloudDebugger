using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace CloudDebugger.Features.AppServices;

public class AppServicesController : Controller
{
    private readonly ILogger<AppServicesController> _logger;

    public AppServicesController(ILogger<AppServicesController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }


    public IActionResult ShowFileSystem()
    {
        _logger.LogInformation("ShowFileSystem action called");

        var model = new ShowFilesModel()
        {
            HomeDirectory = Environment.GetEnvironmentVariable("HOME")
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            model.TempDirectory = Environment.GetEnvironmentVariable("TEMP"); //Windows path
        }
        else
        {
            model.TempDirectory = "/tmp";  //Linux Path
        }

        // https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings
        // https://learn.microsoft.com/en-us/azure/app-service/overview-local-cache
        model.LocalCacheEnabled = Environment.GetEnvironmentVariable("WEBSITE_LOCALCACHE_ENABLED") ?? "Disabled";
        model.LocalCacheReady = Environment.GetEnvironmentVariable("WEBSITE_LOCALCACHE_READY") ?? "False";
        model.LocalCacheOption = Environment.GetEnvironmentVariable("WEBSITE_LOCAL_CACHE_OPTION") ?? "[Not set]";
        model.LocalCacheSize = Environment.GetEnvironmentVariable("WEBSITE_LOCAL_CACHE_SIZEINMB") ?? "Unknown";

        model.AppDirectory = AppContext.BaseDirectory;
        model.OperatingSystem = RuntimeInformation.OSDescription;

        if (model.HomeDirectory != null && Directory.Exists(model.HomeDirectory))
        {
            model.HomeDirFolders = GetFolders(model.HomeDirectory);
            model.HomeDirFiles = GetFiles(model.HomeDirectory);
        }
        else
        {
            model.HomeDirFolders.Add("Directory does not exist or is empty");
        }


        if (model.TempDirectory != null && Directory.Exists(model.TempDirectory))
        {
            model.TempDirFolders = GetFolders(model.TempDirectory);
            model.TempDirFiles = GetFiles(model.TempDirectory);
        }
        else
        {
            model.TempDirFolders.Add("Directory does not exist or is empty");
        }


        if (model.AppDirectory != null && Directory.Exists(model.AppDirectory))
        {
            model.AppDirFolders = GetFolders(model.AppDirectory);
            model.AppDirFiles = GetFiles(model.AppDirectory);
        }
        else
        {
            model.TempDirFiles.Add("Directory does not exist or is empty");
        }

        return View(model);
    }

    private static List<string> GetFiles(string path)
    {
        var result = new List<string>();
        int count = 0;
        foreach (var file in Directory.EnumerateFiles(path).OrderBy(n => n))
        {
            string name = Path.GetFileName(file.TrimEnd(Path.DirectorySeparatorChar));

            result.Add(name);
            if (count++ > 50)
            {
                result.Add(".......");
                break;
            }
        }
        return result;
    }

    private static List<string> GetFolders(string path)
    {
        var result = new List<string>();
        int count = 0;
        foreach (var dir in Directory.EnumerateDirectories(path).OrderBy(n => n))
        {
            string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));

            result.Add(name);
            if (count++ > 50)
            {
                result.Add(".......");
                break;
            }
        }
        return result;
    }
}
