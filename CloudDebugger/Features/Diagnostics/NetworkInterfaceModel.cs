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
    public List<string>? WinsServers { get; set; }
    public List<string>? GatewayAddresses { get; set; }
    public List<IPAddressV4Info>? IPv4AddressInfos { get; set; }
    public List<IPAddressV6Info>? IPv6AddressInfos { get; set; }
}
