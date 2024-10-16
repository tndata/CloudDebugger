using CloudDebugger.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CloudDebugger.Features.FileSystemExplorer;

public class FileSystemExplorerController : Controller
{
    private readonly ILogger<FileSystemExplorerController> _logger;

    public FileSystemExplorerController(ILogger<FileSystemExplorerController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult ReadWriteFiles()
    {
        var model = GetDefaultModel();


        model.DirectoryContent = GetDirectoryContent(model.Path);


        return View(model);
    }

    [HttpPost]
    public IActionResult ReadWriteFiles(ReadWriteFilesModel model, string button)
    {
        model ??= GetDefaultModel();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            model.Message = "";
            model.ErrorMessage = "";
            ModelState.Clear();

            //Remove unwanted characters
            model.Path = model.Path?.SanitizeInput() ?? "";
            model.FileName = model.FileName?.SanitizeInput() ?? "";

            switch (button)
            {
                case "changepath":
                    // Just reload the page with the new path in place.
                    model.FileContent = "";
                    break;
                case "createfolder":
                    CreateDirectory(model);
                    break;
                case "loadfile":
                    model.FileContent = GetFileContent(model);
                    break;
                case "writefile":
                    CreateFile(model);
                    break;
                default:
                    break;
            }

            model.DirectoryContent = GetDirectoryContent(model.Path);
        }
        catch (Exception exc)
        {
            _logger.LogInformation(exc, "Failure when doing a ReadWriteFiles operation");
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
        }

        return View(model);
    }

    private static void CreateFile(ReadWriteFilesModel model)
    {
        if (model.Path != null && model.FileName != null)
        {
            string filePath = Path.Combine(model.Path, model.FileName);

            System.IO.File.WriteAllText(filePath, model.FileContent);
            model.Message = "File created";
        }
        else
        {
            model.ErrorMessage = "File path or name missing";
        }
    }

    private static string? GetFileContent(ReadWriteFilesModel model)
    {
        if (model.Path != null && model.FileName != null)
        {
            string filePath = Path.Combine(model.Path, model.FileName);

            if (System.IO.File.Exists(filePath))
            {
                var content = System.IO.File.ReadAllText(filePath);
                if (content.Length > 10000)
                    content = $"{content.Substring(0, 10000)}\r\n\r\nDisplaying the first 10,000 characters....";
                return content;
            }
            else
            {
                return "File not found.";
            }
        }
        else
        {
            model.ErrorMessage = "File path or name missing";
            return "";
        }
    }

    private static void CreateDirectory(ReadWriteFilesModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        string path = model.Path ?? "";

        if (!string.IsNullOrWhiteSpace(path))
        {
            Directory.CreateDirectory(path);
            model.Message = "Folder created";
        }
        else
        {
            model.ErrorMessage = "Path is empty!";
        }
    }


    private List<(string name, string size)> GetDirectoryContent(string? path)
    {
        var result = new List<(string name, string size)>();

        try
        {
            if (path != null)
            {
                result.AddRange(GetFolders(path));
                result.AddRange(GetFiles(path));
            }
        }
        catch (Exception exc)
        {
            _logger.LogInformation(exc, "Failure when calling GetDirectoryContent for path {Path}", path.SanitizeInput());
            result.Add(("### Error getting the directory content", ""));
        }

        return result;
    }


    private static List<(string name, string size)> GetFiles(string path)
    {
        var result = new List<(string name, string size)>();
        int count = 0;

        // Use DirectoryInfo to get FileInfo objects
        var directoryInfo = new DirectoryInfo(path);
        FileInfo[] files = directoryInfo.GetFiles().OrderBy(f => f.Name).ToArray();

        foreach (var file in files)
        {
            string name = file.Name;
            long fileSize = file.Length;

            result.Add((name, fileSize.ToString()));
            if (count++ > 25)
            {
                result.Add(("Displaying the first 25 files.......", ""));
                break;
            }
        }

        return result;
    }


    private static List<(string name, string size)> GetFolders(string path)
    {
        var result = new List<(string name, string size)>();
        int count = 0;
        foreach (var dir in Directory.EnumerateDirectories(path).OrderBy(n => n))
        {
            string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));

            result.Add(("\\" + name, ""));
            if (count++ > 25)
            {
                result.Add(("\\Displaying the first 25 folders.......", ""));
                break;
            }
        }
        return result;
    }


    /// <summary>
    /// Try to set some sensible default start path and get the App and Home PAth if available. 
    /// </summary>
    /// <returns></returns>
    private static ReadWriteFilesModel GetDefaultModel()
    {
        var model = new ReadWriteFilesModel()
        {
            Message = "",
            ErrorMessage = "",
            DirectoryContent = []
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            //Linux
            model.Path = "/tmp";
        }
        else
        {
            //Windows
            model.Path = Environment.GetEnvironmentVariable("HOME") ?? @"c:\temp";
        }
        model.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "[Unknown]");
        model.HomePath = Environment.GetEnvironmentVariable("HOME") ?? "[Unknown]";

        return model;
    }
}
