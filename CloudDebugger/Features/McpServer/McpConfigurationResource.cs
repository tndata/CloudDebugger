using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace CloudDebugger.Features.McpServer;

[McpServerResourceType]
public class McpConfigurationResource
{
    private static readonly JsonSerializerOptions CachedJsonSerializeOptions = new() { WriteIndented = true };
    private readonly IConfiguration _configuration;

    public McpConfigurationResource(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [McpServerResource(UriTemplate = "config://service/CloudDebugger", Name = "Server Configuration", MimeType = "application/json")]
    [Description("Current configuration settings and parameters as seen by the application")]
    public TextResourceContents GetServerConfiguration()
    {
        var root = (IConfigurationRoot)_configuration;

        // Get configuration data similar to your controller
        var configData = _configuration.AsEnumerable()
            .Where(c => !IsSensitiveConfigKey(c.Key) && c.Value != null)
            .OrderBy(c => c.Key)
            .ToDictionary(c => c.Key, c => c.Value);

        // Get configuration providers information
        var configProviders = root.Providers
            .Select(provider =>
            {
                var childKeys = provider.GetChildKeys([], null).ToList();
                var providerValues = new Dictionary<string, string>();

                // Get values for each key from this provider
                foreach (var key in childKeys)
                {
                    if (provider.TryGet(key, out var value) && value != null && !IsSensitiveConfigKey(key))
                    {
                        providerValues[key] = value;
                    }
                }

                return new
                {
                    Name = provider.ToString(),
                    Type = provider.GetType().Name,
                    ChildKeys = childKeys.Where(k => !IsSensitiveConfigKey(k)).ToList(),
                    Values = providerValues
                };
            })
            .ToList();

        var result = new
        {
            service = "CloudDebugger",
            timestamp = DateTime.UtcNow,
            configuration = configData,
            configuration_providers = configProviders,
            total_settings = configData.Count,
            total_providers = configProviders.Count
        };

        return new TextResourceContents
        {
            Text = JsonSerializer.Serialize(result, CachedJsonSerializeOptions),
            MimeType = "application/json",
            Uri = "config://service/CloudDebugger"
        };
    }

    private static bool IsSensitiveConfigKey(string key)
    {
        var sensitiveKeys = new[] { "ConnectionStrings", "Password", "Secret", "Key", "Token", "ApiKey", "Credential" };
        return Array.Exists(sensitiveKeys, sensitive => key.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
    }
}



