namespace CloudDebugger.Features.Configuration;

public class AppConfigurationModel
{
    public List<KeyValuePair<string, string?>> Config { get; set; } = [];
    public string? DebugView { get; set; }
    public List<ConfigurationProviderDetails> ConfigProviders { get; set; } = [];
}