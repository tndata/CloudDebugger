using CloudDebugger.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudDebugger.Features.Health;

/// <summary>
/// Custom health check to control the health status of the service. 
/// Controls the /health and /healtz endpoints.
/// 
/// https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
/// </summary>
public class AppHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return DebuggerSettings.ServiceHealth switch
        {
            HealthStatusEnum.Healthy => Task.FromResult(HealthCheckResult.Healthy("OK")),
            HealthStatusEnum.Degraded => Task.FromResult(HealthCheckResult.Degraded("Degraded")),
            HealthStatusEnum.Unhealthy => Task.FromResult(HealthCheckResult.Unhealthy("Unhealthy")),
            _ => Task.FromResult(HealthCheckResult.Unhealthy("Unhealthy")),
        };
    }
}
