using ModelContextProtocol.Server;
using System.Collections;
using System.ComponentModel;
using System.Text.Json;

namespace CloudDebugger.Features.McpServer;

[McpServerResourceType]
public static class McpEnvironmentResources
{
    private static readonly JsonSerializerOptions CachedJsonSerializeOptions = new() { WriteIndented = true };

    [McpServerResource(UriTemplate = "env://service/CloudDebugger", Name = "Environment Variables", MimeType = "application/json")]
    [Description("Current environment variables and system settings")]
    public static string GetEnvironmentVariables()
    {
        var environmentVariables = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Where(entry => !IsSensitiveKey(entry.Key?.ToString() ?? string.Empty))
            .ToDictionary(entry => entry.Key?.ToString() ?? string.Empty, entry => entry.Value?.ToString() ?? string.Empty);

        var result = new
        {
            service = "CloudDebugger",
            timestamp = DateTime.UtcNow,
            environment_variables = environmentVariables,
            total_count = environmentVariables.Count
        };

        return JsonSerializer.Serialize(result, CachedJsonSerializeOptions);
    }

    private static bool IsSensitiveKey(string key)
    {
        var sensitiveKeys = new[] { "PASSWORD", "SECRET", "KEY", "TOKEN", "CONN", "API_KEY" };
        return Array.Exists(sensitiveKeys, sensitive => key.Contains(sensitive, StringComparison.CurrentCultureIgnoreCase));
    }
}
