namespace CloudDebugger.Features.Health;

public class HealthModel
{
    public HealthStatusEnum CurrentServiceHealth { get; set; }
    public CustomHealthStatusEnum CurrentCustomHealth { get; set; }
}