namespace CloudDebugger.Features.AppServices
{
    public class BenchmarkModel
    {
        public string? CachablePath { get; set; }
        public string? NonCachablePath { get; set; }
        public string? TempPath { get; set; }

        public List<string>? ResultLog { get; set; } = new();
    }
}
