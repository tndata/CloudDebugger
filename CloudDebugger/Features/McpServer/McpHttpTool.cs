using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

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
    public static Task<object> HttpGet(Uri targetUrl, CancellationToken cancellationToken = default)
        => HttpGetCoreAsync(targetUrl, _http, cancellationToken);

    internal static async Task<object> HttpGetCoreAsync(Uri targetUrl, HttpClient httpClient, CancellationToken cancellationToken)
    {
        try
        {
            DateTime startTime = DateTime.UtcNow;
            long startTimestamp = Stopwatch.GetTimestamp();
            using HttpResponseMessage response = await httpClient.GetAsync(targetUrl, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
            var contentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            return new McpHttpSuccessResponse(
                TargetUrl: targetUrl,
                StatusCode: (int)response.StatusCode,
                StatusText: response.StatusCode.ToString(),
                IsSuccess: response.IsSuccessStatusCode,
                Content: content,
                ContentType: response.Content.Headers.ContentType?.ToString(),
                ResponseHeaders: headers,
                ContentHeaders: contentHeaders,
                RequestDurationMs: Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
                Timestamp: startTime);
        }
        catch (HttpRequestException ex)
        {
            return new McpHttpErrorResponse(
                Error: $"HTTP request failed: {ex.Message}",
                TargetUrl: targetUrl,
                ExceptionType: nameof(HttpRequestException));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Let the MCP SDK observe cancellation of the agent's request.
            throw;
        }
        catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return new McpHttpErrorResponse(
                Error: "Request timed out after 30 seconds",
                TargetUrl: targetUrl,
                ExceptionType: nameof(TimeoutException));
        }
        catch (OperationCanceledException ex)
        {
            return new McpHttpErrorResponse(
                Error: "Request was cancelled",
                TargetUrl: targetUrl,
                ExceptionType: ex.GetType().Name);
        }
        catch (UriFormatException ex)
        {
            return new McpHttpErrorResponse(
                Error: $"Invalid URL format: {ex.Message}",
                TargetUrl: targetUrl,
                ExceptionType: nameof(UriFormatException));
        }
        catch (Exception ex)
        {
            return new McpHttpErrorResponse(
                Error: $"Unexpected error: {ex.Message}",
                TargetUrl: targetUrl,
                ExceptionType: ex.GetType().Name);
        }
    }
}

