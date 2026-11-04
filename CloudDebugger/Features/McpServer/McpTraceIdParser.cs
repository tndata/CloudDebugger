using System.Diagnostics;

namespace CloudDebugger.Features.McpServer;

internal static class McpTraceIdParser
{
    internal static ActivityTraceId Parse(string? traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            throw new ArgumentException("Trace ID cannot be empty", nameof(traceId));
        }

        string normalized;
        if (traceId.Length == 32 && traceId.All(char.IsAsciiHexDigit))
        {
            normalized = traceId.ToLowerInvariant();
        }
        else if (traceId.Length == 36 && Guid.TryParseExact(traceId, "D", out Guid guid))
        {
            normalized = guid.ToString("N");
        }
        else
        {
            throw new FormatException("Invalid trace ID format. Expected 32 hexadecimal characters or a dashed GUID.");
        }

        if (normalized == "00000000000000000000000000000000")
        {
            throw new FormatException("Trace ID cannot be all zeros.");
        }

        return ActivityTraceId.CreateFromString(normalized.AsSpan());
    }
}
