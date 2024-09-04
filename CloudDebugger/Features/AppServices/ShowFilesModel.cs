namespace CloudDebugger.Features.AppServices;

public class ShowFilesModel
{
    public string? HomeDirectory { get; set; }
    public string? TempDirectory { get; set; }
    public string? AppDirectory { get; set; }
    public string? OperatingSystem { get; set; }


    public string? LocalCacheEnabled { get; set; }
    public string? LocalCacheReady { get; set; }
    public string? LocalCacheOption { get; set; }
    public string? LocalCacheSize { get; set; }

    public List<string> HomeDirFolders { get; set; } = [];
    public List<string> HomeDirFiles { get; set; } = [];

    public List<string> TempDirFolders { get; set; } = [];
    public List<string> TempDirFiles { get; set; } = [];

    public List<string> AppDirFolders { get; set; } = [];
    public List<string> AppDirFiles { get; set; } = [];
}
