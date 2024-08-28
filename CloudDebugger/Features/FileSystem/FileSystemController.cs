using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.FileSystem;

public class FileSystemController : Controller
{
    private readonly ILogger<FileSystemController> _logger;

    public FileSystemController(ILogger<FileSystemController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult ReadWriteFiles()
    {
        var model = new ReadWriteFilesModel();

        model.DirectoryContent = GetDirectoryContent(model.Path);
        model.AppPath = AppContext.BaseDirectory;
        model.HomePath = Environment.GetEnvironmentVariable("HOME") ?? "[Unknown]";


        return View(model);
    }

    [HttpPost]
    public IActionResult ReadWriteFiles(ReadWriteFilesModel model, string button)
    {
        if (model == null)
            return View(new ReadWriteFilesModel());

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        try
        {
            switch (button)
            {
                case "changepath":
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
                    content = content.Substring(0, 10000) + "\r\n\r\nDisplaying the first 10,000 characters....";
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
        if (string.IsNullOrWhiteSpace(model.Path) == false)
        {
            Directory.CreateDirectory(model.Path);
            model.Message = "Folder created";
        }
        else
        {
            model.ErrorMessage = "Path is empty!";
        }
    }


    private static List<(string name, string size)> GetDirectoryContent(string? path)
    {
        var result = new List<(string name, string size)>();

        if (path != null)
        {
            result.AddRange(GetFolders(path));
            result.AddRange(GetFiles(path));
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
}
