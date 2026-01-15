using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace CloudDebugger.Features.AppServices;

public class AppServicesController : Controller
{
    private readonly ILogger<AppServicesController> _logger;

    // Limit displayed items to keep the UI responsive and page load times reasonable
    private const int MaxNumberOfDirectories = 50;
    private const int MaxNumberOfFiles = 100;

    public AppServicesController(ILogger<AppServicesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// References
    /// https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings
    /// https://learn.microsoft.com/en-us/azure/app-service/overview-local-cache
    /// </summary>
    /// <returns></returns>
    public IActionResult LocalCache()
    {
        var notSet = "[Not set]";

        var model = new LocalCacheModel()
        {
            LocalCacheEnabled = Environment.GetEnvironmentVariable("WEBSITE_LOCALCACHE_ENABLED") ?? notSet,
            LocalCacheReady = Environment.GetEnvironmentVariable("WEBSITE_LOCALCACHE_READY") ?? notSet,

            LocalCacheOption = Environment.GetEnvironmentVariable("WEBSITE_LOCAL_CACHE_OPTION") ?? notSet,
            LocalCacheSize = Environment.GetEnvironmentVariable("WEBSITE_LOCAL_CACHE_SIZEINMB") ?? notSet,
            LocalCacheReadWriteOptions = Environment.GetEnvironmentVariable("WEBSITE_LOCAL_CACHE_READWRITE_OPTION") ?? notSet,

            WebSiteVolumeType = Environment.GetEnvironmentVariable("WEBSITE_VOLUME_TYPE") ?? notSet
        };

        return View(model);
    }



    public IActionResult ShowFileSystem()
    {
        _logger.LogInformation("ShowFileSystem action called");

        var model = new ShowFilesModel()
        {
            AppDirectory = AppContext.BaseDirectory,
            OperatingSystem = RuntimeInformation.OSDescription,
            HomeDirectory = GetHomeDirectory(),
            TempDirectory = GetTempDirectory()
        };

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

    private static string? GetTempDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Environment.GetEnvironmentVariable("TEMP"); //Windows path
        }
        else
        {
            return "/tmp";  //Linux Path. The env variable is not set for Linux services.
        }
    }

    private static string GetHomeDirectory()
    {
        var homeDirectory = Environment.GetEnvironmentVariable("HOME");

        if (string.IsNullOrEmpty(homeDirectory))
        {
            //Set the default home directory, if missing or empty
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                homeDirectory = "c:\\home";
            }
            else
            {
                homeDirectory = "/home";
            }
        }
        return homeDirectory;
    }

    private static List<string> GetFiles(string path)
    {
        var result = new List<string>();
        try
        {
            int count = 0;
            foreach (var file in Directory.EnumerateFiles(path).OrderBy(n => n))
            {
                string name = Path.GetFileName(file.TrimEnd(Path.DirectorySeparatorChar));

                result.Add(name);
                if (count++ > MaxNumberOfFiles)
                {
                    result.Add($"...Displays the first {MaxNumberOfFiles} files....");
                    break;
                }
            }
        }
        catch (Exception exc)
        {
            result.Add($"An exception occurred getting the files: {exc.Message}");
        }

        return result;
    }

    private static List<string> GetFolders(string path)
    {
        var result = new List<string>();

        try
        {
            int count = 0;
            foreach (var dir in Directory.EnumerateDirectories(path).OrderBy(n => n))
            {
                string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));

                result.Add($"{Path.DirectorySeparatorChar}{name}");
                if (count++ > MaxNumberOfDirectories)
                {
                    result.Add($"...Displays the first {MaxNumberOfDirectories} directories....");
                    break;
                }
            }
        }
        catch (Exception exc)
        {
            result.Add($"An exception occurred getting the folders: {exc.Message}");
        }

        return result;
    }
}
