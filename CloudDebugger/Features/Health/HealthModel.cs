namespace CloudDebugger.Features.Health;

public class HealthModel
{
    public HealthStatusEnum CurrentServiceHealth { get; set; } = HealthStatusEnum.Unhealthy;
    public CustomHealthStatusEnum CurrentCustomHealth { get; set; } = CustomHealthStatusEnum.Error;
}