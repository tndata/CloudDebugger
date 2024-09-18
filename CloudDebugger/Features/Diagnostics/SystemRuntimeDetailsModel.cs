namespace CloudDebugger.Features.Diagnostics;

public class SystemRuntimeDetailsModel
{
    public string? FrameworkDescription { get; set; }
    public string? OSArchitecture { get; set; }
    public string? OSDescription { get; set; }
    public string? ProcessArchitecture { get; set; }
    public string? RuntimeIdentifier { get; set; }


    public string? RuntimeDirectory { get; set; }
    public string? SystemVersion { get; set; }
    public string? RunningInContainer { get; set; }

    public List<string> ServerAddresses { get; set; } = [];

}