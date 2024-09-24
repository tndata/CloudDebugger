namespace CloudDebugger.Features.Diagnostics;

public class NetworkInterfaceModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? NetworkInterfaceType { get; set; }
    public string? OperationalStatus { get; set; }
    public string? IPVersions { get; set; }
    public string? DnsSuffix { get; set; }
    public List<string>? DnsAddresses { get; set; }
    public List<string>? UnicastAddresses { get; set; }
    public List<string>? WinsServers { get; set; }
}
