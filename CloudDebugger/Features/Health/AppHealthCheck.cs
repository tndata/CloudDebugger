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

        switch (Settings.ServiceHealth)
        {
            case HealthStatusEnum.Healthy:
                return Task.FromResult(
                    HealthCheckResult.Healthy("OK"));
            case HealthStatusEnum.Degraded:
                return Task.FromResult(
                    HealthCheckResult.Degraded("Degraded"));
            case HealthStatusEnum.Unhealthy:
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("Unhealthy"));
            default:
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("Unhealthy"));
        }
    }
}
