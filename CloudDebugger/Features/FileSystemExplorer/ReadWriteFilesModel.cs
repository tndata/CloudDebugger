namespace CloudDebugger.Features.FileSystemExplorer;

public class ReadWriteFilesModel
{
    public string? Path { get; set; }
    public string? FileName { get; set; } = "test.txt";
    public string? FileContent { get; set; } = "Current time is " + DateTime.UtcNow;

    public string? HomePath { get; set; }
    public string? AppPath { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

    public List<(string name, string size)>? DirectoryContent { get; set; }
}
