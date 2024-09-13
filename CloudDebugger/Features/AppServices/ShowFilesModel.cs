namespace CloudDebugger.Features.AppServices;

public class ShowFilesModel
{
    public string? HomeDirectory { get; set; }
    public string? TempDirectory { get; set; }

    /// <summary>
    /// The value returned is the containing directory of the host executable.
    /// </summary>
    public string? AppDirectory { get; set; }

    /// <summary>
    /// Describes the operating system on which the app is running.
    /// </summary>
    public string? OperatingSystem { get; set; }

    public List<string> HomeDirFolders { get; set; } = [];
    public List<string> HomeDirFiles { get; set; } = [];

    public List<string> TempDirFolders { get; set; } = [];
    public List<string> TempDirFiles { get; set; } = [];

    public List<string> AppDirFolders { get; set; } = [];
    public List<string> AppDirFiles { get; set; } = [];
}
