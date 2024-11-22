namespace CloudDebugger.Features.Configuration;

public class ConfigurationProviderDetails
{
    public string? Name { get; set; }
    public string? Prefix { get; set; }
    public List<string> ChildKeys { get; set; } = [];
    public Dictionary<string, string?> Values { get; set; } = [];
}