namespace CloudDebugger.Features.ScaleOut;

public class ScaleOutModel
{
    public string? SiteName { get; internal set; }
    public string? HostName { get; internal set; }
    public string? PodIP { get; internal set; }
    public string? IPAddress { get; internal set; }
    public string? InstanceId { get; internal set; }
    public string? ComputerName { get; internal set; }
    public string? BackgroundColor { get; internal set; }
    public string? CurrentTime { get; internal set; }
    public TimeSpan RunningTime { get; internal set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(SiteName, HostName, PodIP, IPAddress, InstanceId, ComputerName);
    }
}