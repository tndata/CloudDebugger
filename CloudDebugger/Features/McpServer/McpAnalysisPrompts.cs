using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CloudDebugger.Features.McpServer;

[McpServerPromptType]
public static class McpAnalysisPrompts
{
    [McpServerPrompt(Name = "analyze-trace")]
    [Description("Analyze OpenTelemetry trace for performance issues, bottlenecks, and failures")]
    public static IEnumerable<ChatMessage> AnalyzeTrace([Description("Trace ID to analyze")] string TraceId)
    {
        if (string.IsNullOrWhiteSpace(TraceId))
        {
            return [
                new ChatMessage(ChatRole.User, "Please provide a trace ID to analyze. Use the format: analyze-trace <trace_id>")
            ];
        }

        var prompt = $@"Analyze OpenTelemetry trace {TraceId} for CloudDebugger service. 

Please fetch the trace data using the available resources and provide a comprehensive analysis including:

1. **Trace Overview**
   - Total duration and span count
   - Critical path identification
   - Service dependencies

2. **Performance Analysis**
   - Identify the slowest operations (top 5)
   - Calculate time spent in each operation type
   - Find operations exceeding expected thresholds

3. **Error Analysis**
   - List all failed spans with error details
   - Identify error patterns and cascading failures
   - Root cause analysis for failures

4. **Bottleneck Detection**
   - Sequential operations that could be parallelized
   - Long-running database queries or API calls
   - Resource contention indicators

5. **Optimization Recommendations**
   - Specific code-level improvements
   - Architecture suggestions
   - Caching opportunities
   - Query optimization needs

Format the analysis in a clear, actionable report with specific metrics and examples from the trace data.";

        return [
            new ChatMessage(ChatRole.User, prompt)
        ];
    }

    [McpServerPrompt(Name = "analyze-logs")]
    [Description("Analyze recent logs for errors, warnings, and performance patterns")]
    public static IEnumerable<ChatMessage> AnalyzeLogs(
        [Description("Number of minutes to analyze")] int minutes = 5)
    {
        var prompt = $@"Analyze the CloudDebugger service logs from the last {minutes} minutes.

Please fetch the latest log data using the available resources and provide a comprehensive analysis including:

1. **Log Summary**
   - Total log entries by level (Error, Warning, Info, Debug)
   - Log volume trends over the time period
   - Most active components/categories

2. **Error Pattern Analysis**
   - Group similar errors together
   - Identify recurring error messages
   - Calculate error frequency and trends
   - Correlate errors with specific operations or traces

3. **Warning Analysis**
   - Common warning patterns
   - Warnings that preceded errors
   - Resource usage warnings (memory, CPU, connections)

4. **Performance Indicators**
   - Slow operation logs
   - Timeout occurrences
   - Queue depth or backpressure warnings
   - Database connection pool issues

5. **Root Cause Analysis**
   - For each major error pattern, identify:
     * Probable root cause
     * Impact assessment
     * Related traces for deeper investigation

6. **Actionable Remediation Steps**
   - Immediate actions to address critical errors
   - Configuration changes needed
   - Code fixes required
   - Monitoring improvements

7. **Correlation Insights**
   - Link errors to specific trace IDs
   - Identify cascading failure patterns
   - User impact assessment

Please structure the analysis as a prioritized action plan, starting with the most critical issues.";

        return [
            new ChatMessage(ChatRole.User, prompt)
        ];
    }

    [McpServerPrompt(Name = "correlate-trace-logs")]
    [Description("Correlate trace and log data for comprehensive debugging")]
    public static IEnumerable<ChatMessage> CorrelateTraceLogs(
        [Description("Trace ID to correlate with logs")] string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return [
                new ChatMessage(ChatRole.User, "Please provide a trace ID to correlate with logs. Use the format: correlate-trace-logs <trace_id>")
            ];
        }

        var prompt = $@"Perform a correlated analysis of trace {traceId} with its associated logs.

Please fetch both the trace data and related logs, then provide:

1. **Timeline Reconstruction**
   - Create a unified timeline of spans and log entries
   - Highlight critical events and state changes

2. **Error Correlation**
   - Match error logs to specific spans
   - Identify error propagation through the trace

3. **Performance Correlation**
   - Link slow spans with warning/info logs
   - Identify resource constraints from logs during slow operations

4. **Debugging Insights**
   - Combine trace and log data to pinpoint failure points
   - Provide a step-by-step breakdown of what went wrong

5. **Recommendations**
   - Specific fixes based on correlated data
   - Additional logging or tracing needed

This correlated view should provide a complete picture of the request flow and any issues encountered.";

        return [
            new ChatMessage(ChatRole.User, prompt)
        ];
    }

    [McpServerPrompt(Name = "quick-health-check")]
    [Description("Quick health check of the service based on recent traces and logs")]
    public static IEnumerable<ChatMessage> QuickHealthCheck()
    {
        return [
            new ChatMessage(ChatRole.User, @"Perform a quick health check of the CloudDebugger service.

Fetch the latest traces and logs, then provide a concise health report:

1. **Current Status** (🟢 Healthy / 🟡 Warning / 🔴 Critical)
2. **Error Rate** - Last 5 minutes
3. **Performance** - Average response time
4. **Active Issues** - Top 3 current problems
5. **Recommended Actions** - Immediate steps if any

Keep the response brief and actionable."),
            new ChatMessage(ChatRole.Assistant, "I'll perform a quick health check of the CloudDebugger service by analyzing recent traces and logs."),
            new ChatMessage(ChatRole.User, "Please fetch the data and provide the health status report.")
        ];
    }
}