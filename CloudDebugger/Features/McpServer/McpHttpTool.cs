using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CloudDebugger.Features.McpServer;


[McpServerToolType]
public static class McpHttpTools
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30) // 30 second timeout
    };

    static McpHttpTools()
    {
        // Set a reasonable user agent
        _http.DefaultRequestHeaders.Add("User-Agent", "CloudDebugger-MCP/1.0");
    }

    [McpServerTool(Name = "http_get")]
    [Description("Make an HTTP GET request to the specified URL and return the response")]
    public static async Task<object> HttpGet(string targetUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return new
                {
                    success = false,
                    error = "URL cannot be empty",
                    targetUrl
                };
            }

            if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
            {
                return new
                {
                    success = false,
                    error = "Invalid URL format",
                    targetUrl
                };
            }

            var startTime = DateTime.UtcNow;
            var response = await _http.GetAsync(uri);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var content = await response.Content.ReadAsStringAsync();
            var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
            var contentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            return new
            {
                success = true,
                targetUrl,
                status_code = (int)response.StatusCode,
                status_text = response.StatusCode.ToString(),
                is_success = response.IsSuccessStatusCode,
                content,
                content_length = content.Length,
                content_type = response.Content.Headers.ContentType?.ToString(),
                response_headers = headers,
                content_headers = contentHeaders,
                request_duration_ms = duration.TotalMilliseconds,
                timestamp = startTime
            };
        }
        catch (HttpRequestException ex)
        {
            return new
            {
                success = false,
                error = $"HTTP request failed: {ex.Message}",
                targetUrl,
                exception_type = "HttpRequestException"
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return new
            {
                success = false,
                error = "Request timed out after 30 seconds",
                targetUrl,
                exception_type = "TimeoutException"
            };
        }
        catch (TaskCanceledException)
        {
            return new
            {
                success = false,
                error = "Request was cancelled",
                targetUrl,
                exception_type = "TaskCanceledException"
            };
        }
        catch (UriFormatException ex)
        {
            return new
            {
                success = false,
                error = $"Invalid URL format: {ex.Message}",
                targetUrl,
                exception_type = "UriFormatException"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                success = false,
                error = $"Unexpected error: {ex.Message}",
                targetUrl,
                exception_type = ex.GetType().Name
            };
        }
    }
}

