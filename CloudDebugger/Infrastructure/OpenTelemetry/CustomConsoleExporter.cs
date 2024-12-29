using OpenTelemetry;
using OpenTelemetry.Logs;
using System.Globalization;
using System.Text;

namespace CloudDebugger.Infrastructure.OpenTelemetry;

/// <summary>
/// Custom OpenTelemetry logger that logs to the console.
///
/// This custom logger provides a more familiar logging output to the console, similar to Serilog output.
/// The default Console.Exporter is not suitable for this application due to its output format.
///
/// For more information, see:
/// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/ConsoleLogRecordExporter.cs
/// </summary>
public class CustomConsoleExporter : BaseExporter<LogRecord>
{
    private readonly Lock lockObject = new();

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        // Ensuring that we don't write logs to the console from multiple threads
        lock (lockObject)
        {
            foreach (var record in batch)
            {
                var timestamp = record.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                var level = record.LogLevel switch

                {
                    LogLevel.Information => "INF",
                    LogLevel.Debug => "DBG",
                    LogLevel.Trace => "TRC",
                    LogLevel.Warning => "WRN",
                    LogLevel.Error => "ERR",
                    LogLevel.Critical => "CRT",
                    LogLevel.None => "NON",
                    _ => "UNK"
                };
                var sourceContext = record.CategoryName;


                // Prefer FormattedMessage if available; fallback to manually rendering Body
                var message = record.FormattedMessage ?? PopulateMessageTemplate(record.Body, record.Attributes);

                Console.WriteLine($"[{timestamp} {level}] {sourceContext} {message}");
                if (record.Exception != null)
                {
                    Console.WriteLine(record.Exception);
                }
            }
        }

        return ExportResult.Success;
    }

    private static string PopulateMessageTemplate(string? template,
                                                  IReadOnlyCollection<KeyValuePair<string, object?>>? attributes)
    {
        if (string.IsNullOrWhiteSpace(template) || attributes is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(template);
        foreach (var attribute in attributes)
        {
            sb.Replace($"{{{attribute.Key}}}", attribute.Value?.ToString() ?? "null");
        }

        return sb.ToString();
    }
}
