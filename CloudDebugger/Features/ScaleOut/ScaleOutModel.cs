namespace CloudDebugger.Features.ScaleOut;

public record ScaleOutModel
{
    public string? SiteName { get; set; }
    public string? HostName { get; set; }
    public string? PodIP { get; set; }
    public string? IPAddress { get; set; }
    public string? InstanceId { get; set; }
    public string? ComputerName { get; set; }
    public string? BackgroundColor { get; set; }
    public string? CurrentTime { get; set; }
    public TimeSpan RunningTime { get; set; }

    // These are taken from the environment variables. Used for example when experimenting with App Settings And Deployment Slots
    public string? Version { get; set; }
    public string? ConStr { get; set; }
    public string? Env { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(SiteName, HostName, PodIP, IPAddress, InstanceId, ComputerName);
    }
}