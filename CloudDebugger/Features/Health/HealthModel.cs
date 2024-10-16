namespace CloudDebugger.Features.Health;

public class HealthModel
{
    /// <summary>
    ///  The status of the health endpoints, as used by the ASP.NET Core built in Health Checks Middleware.
    /// </summary>
    public HealthStatusEnum CurrentServiceHealth { get; set; } = HealthStatusEnum.Unhealthy;

    /// <summary>
    /// This controls the state of the custom health endpoint.
    /// </summary>
    public CustomHealthStatusEnum CurrentCustomHealth { get; set; } = CustomHealthStatusEnum.Error;
}