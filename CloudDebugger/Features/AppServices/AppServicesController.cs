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
        var model = new ShowFilesModel()
        {
            HomeDirectory = Environment.GetEnvironmentVariable("HOME")
        };

        model.TempDirectory = "/tmp"; //Linux Path
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            model.TempDirectory = Environment.GetEnvironmentVariable("TEMP"); //Windows path
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




/* This part, trying to demonstate the App Service Local cache feature, does not really show any major difference
 *

public IActionResult Benchmark()
{
    var model = new BenchmarkModel();

    // https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings
    string localCacheReady = Environment.GetEnvironmentVariable("WEBSITE_LOCALCACHE_READY") ?? "Disabled";
    ViewData["localCacheReady"] = localCacheReady;


    string homePath = AppContext.BaseDirectory;


    var sw = new Stopwatch();
    sw.Start();

    string totalHash = "";
    int iterations = 25;
    for (int i = 0; i < iterations; i++)
    {
        totalHash = CalculateTotalHash(homePath);
    }
    sw.Stop();
    var str = $"\r\nCalculating the hash of the entire application {iterations} times, took {sw.Elapsed.TotalSeconds} sec.";

    model.ResultLog.Add(str);
    model.ResultLog.Add($"Hash = {totalHash}");


    //Console.WriteLine("Total hash/checksum of all files: " + totalHash);



    model.CachablePath = homePath;
    model.NonCachablePath = "";
    model.TempPath = "";

    return View(model);
}

private string CalculateTotalHash(string directoryPath)
{
    using (SHA256 sha256 = SHA256.Create())
    {
        // Initialize a StringBuilder to store the combined hash
        StringBuilder combinedHash = new StringBuilder();

        // Get all files in the directory
        string[] files = Directory.GetFiles(directoryPath);

        foreach (string file in files)
        {
            // Read the file content
            byte[] fileContent = System.IO.File.ReadAllBytes(file);

            // Compute the hash of the file content
            byte[] fileHash = sha256.ComputeHash(fileContent);

            // Convert the hash to a hexadecimal string and append it to the combined hash
            combinedHash.Append(BitConverter.ToString(fileHash).Replace("-", "").ToLowerInvariant());
        }

        // Compute the final hash of the combined hash string
        byte[] finalHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedHash.ToString()));

        // Convert the final hash to a hexadecimal string
        return BitConverter.ToString(finalHash).Replace("-", "").ToLowerInvariant();
    }
}


public IActionResult Benchmark2()
{
    var model = new BenchmarkModel();

    string localCacheReady = Environment.GetEnvironmentVariable("WEBSITE_LOCALCACHE_READY") ?? "Disabled";
    ViewData["localCacheReady"] = localCacheReady;

    //var homePath = Environment.GetEnvironmentVariable("HOME");

    //homePath = "c:\\HomePath";//For testing purposes

    string homePath = AppContext.BaseDirectory;




    string fileName = $"LARGEFILE-{Environment.TickCount.ToString()}.TXT";
    string cachablePath = Path.Combine(homePath, "test", fileName);
    string nonCachablePath = Path.Combine(homePath, "repository", fileName);
    var tmpPath = Path.Combine(homePath, "/tmp", fileName); //Linux Path

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        var tmp = Environment.GetEnvironmentVariable("TEMP") ?? "";
        tmpPath = Path.Combine(tmp, fileName) ?? ""; //Windows path
    }


    //tmpPath = "c:\\temp"; //For testing purposes


    _logger.LogInformation($"cachablePath={cachablePath}");
    _logger.LogInformation($"nonCachablePath={nonCachablePath}");
    _logger.LogInformation($"TempPath={tmpPath}");

    //model.ResultLog.Add(WriteLargeFile(cachablePath));
    //model.ResultLog.Add(WriteLargeFile(nonCachablePath));
    //model.ResultLog.Add(WriteLargeFile(tmpPath));
    //model.ResultLog.Add("");
    model.ResultLog.Add(BenchmarkFile(cachablePath, 15));
    //model.ResultLog.Add(BenchmarkFile(nonCachablePath, 10));
    model.ResultLog.Add(BenchmarkFile(tmpPath, 15));
    model.ResultLog.Add("Benchmark done");
    _logger.LogInformation("Benchmark done");

    model.CachablePath = cachablePath;
    model.NonCachablePath = nonCachablePath;
    model.TempPath = tmpPath;

    return View(model);
}

private string WriteLargeFile(string pathAndFileName)
{
    try
    {

        // Create a 100 MB file
        byte[] data = new byte[100 * 1024 * 1024];
        new Random().NextBytes(data);

        System.IO.File.WriteAllBytes(pathAndFileName, data);

        var str = $"\r\nWrote a 100 MB file to '{pathAndFileName}'";
        _logger.LogInformation(str);
        return str;
    }
    catch (Exception ex)
    {
        string str = $"\r\nFailed to write file to '{pathAndFileName}'\r\nException={ex.Message}";
        _logger.LogError(ex, str);
        return str;
    }

}






private string BenchmarkFile(string path, int iterations)
{
    // Extract the directory path from the full file path
    string directoryPath = Path.GetDirectoryName(path);

    // Ensure the directory exists
    if (directoryPath != null)
    {
        Directory.CreateDirectory(directoryPath);
    }


    var sw = new Stopwatch();
    sw.Start();
    try
    {
        for (int i = 0; i < iterations; i++)
        {
            // Create a 100 MB file
            byte[] data = new byte[100 * 1024 * 1024];
            new Random().NextBytes(data);

            System.IO.File.WriteAllBytes(path, data);


            System.IO.File.ReadAllBytes(path);
        }
        sw.Stop();
        var str = $"\r\nReading file from '{path}' {iterations} times, took {sw.Elapsed.TotalSeconds} sec.";
        _logger.LogInformation(str);
        return str;
    }
    catch (Exception ex)
    {
        string str = $"\r\nFailed to read from '{path}'\r\nException={ex.Message}";
        _logger.LogError(ex, str);
        return str;
    }
}
}
*/
/*
* If you're using the Local Cache feature with Staging Environments, the swap operation does not complete until Local Cache is warmed up. 
* To check if your site is running against Local Cache, you can check the worker process environment variable WEBSITE_LOCALCACHE_READY.
* Use the instructions on the worker process environment variable page to access the worker process environment variable on multiple instances.*/
