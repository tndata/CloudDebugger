namespace CloudDebugger.Features.Diagnostics;

public class NetworkDetailsModel
{
    public string? HostName { get; set; }
    public string? DomainName { get; set; }
    public List<NetworkInterfaceModel>? NetworkInterfaces { get; set; }
}
