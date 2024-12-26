namespace CloudDebugger.Infrastructure.OpenTelemetry;

/// <summary>
/// Provides static methods for logging at various levels and ensures structured logging templates and values.
/// 
/// Similar to the Serilog global Log type.
/// </summary>
public static class Log
{
    private static ILogger? _logger;

    public static void Configure(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("GlobalLogger");
    }

    public static ILogger Logger => _logger ?? throw new InvalidOperationException("Logger is not configured. Call Log.Configure() first.");

#pragma warning disable IDE0079 //Remove unnecessary suppression 
#pragma warning disable CA2254 // Template should be a static expression

    [MessageTemplateFormatMethod("messageTemplate")]
    public static void Write(LogLevel level, string messageTemplate, params object[] args)
    {
        // Ensures structured logging templates and values
        Logger.Log(level, messageTemplate, args);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public static void Write(LogLevel level, Exception exception, string messageTemplate, params object[] args)
    {
        // Ensures structured logging with exception support
        Logger.Log(level, exception, messageTemplate, args);
    }


    public static void Information(string messageTemplate, params object[] args) => Write(LogLevel.Information, messageTemplate, args);

    public static void Warning(string messageTemplate, params object[] args) => Write(LogLevel.Warning, messageTemplate, args);

    public static void Error(string messageTemplate, params object[] args) => Write(LogLevel.Error, messageTemplate, args);

    public static void Debug(string messageTemplate, params object[] args) => Write(LogLevel.Debug, messageTemplate, args);

    public static void Critical(string messageTemplate, params object[] args) => Write(LogLevel.Critical, messageTemplate, args);

    public static void Critical(Exception exception, string messageTemplate, params object[] args) => Logger.LogCritical(exception, messageTemplate, args);
#pragma warning restore CA2254 // Template should be a static expression
#pragma warning restore IDE0079
}
