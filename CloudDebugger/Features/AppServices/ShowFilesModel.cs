namespace CloudDebugger.Features.AppServices
{
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

        public List<string> HomeDirFolders = [];
        public List<string> HomeDirFiles = [];

        public List<string> TempDirFolders = [];
        public List<string> TempDirFiles = [];

        public List<string> AppDirFolders = [];
        public List<string> AppDirFiles = [];
    }
}
