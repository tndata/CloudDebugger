namespace CloudDebugger.Features.Diagnostics;

public class IPAddressV4Info
{
    public string IPAddress { get; set; }
    public string SubnetMask { get; set; }
}

public class IPAddressV6Info
{
    public string IPAddress { get; set; }
    public string PrefixLength { get; set; }
}