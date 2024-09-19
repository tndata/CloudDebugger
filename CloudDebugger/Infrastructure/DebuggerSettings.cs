using CloudDebugger.Features.Health;

namespace CloudDebugger.Infrastructure;

public static class DebuggerSettings
{
    /// <summary>
    /// The date and time when this service was started
    /// </summary>
    public static DateTime StartupTime { get; set; }

    /// <summary>
    /// Controls the ASP.NET Core health endpoint status
    /// </summary>
    public static HealthStatusEnum ServiceHealth { get; set; } = HealthStatusEnum.Healthy;
}
