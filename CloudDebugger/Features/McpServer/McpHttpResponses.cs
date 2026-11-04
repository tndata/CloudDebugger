using System.Text.Json.Serialization;

namespace CloudDebugger.Features.McpServer;

internal sealed record McpHttpSuccessResponse(
    [property: JsonPropertyName("targetUrl")] Uri TargetUrl,
    [property: JsonPropertyName("status_code")] int StatusCode,
    [property: JsonPropertyName("status_text")] string StatusText,
    [property: JsonPropertyName("is_success")] bool IsSuccess,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("content_type")] string? ContentType,
    [property: JsonPropertyName("response_headers")] IReadOnlyDictionary<string, string> ResponseHeaders,
    [property: JsonPropertyName("content_headers")] IReadOnlyDictionary<string, string> ContentHeaders,
    [property: JsonPropertyName("request_duration_ms")] double RequestDurationMs,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp)
{
    [JsonPropertyName("success")]
    public bool Success { get; } = true;

    [JsonPropertyName("content_length")]
    public int ContentLength => Content.Length;
}

internal sealed record McpHttpErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("targetUrl")] Uri TargetUrl,
    [property: JsonPropertyName("exception_type")] string ExceptionType)
{
    [JsonPropertyName("success")]
    public bool Success { get; } = false;
}
