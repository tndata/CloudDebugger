using Azure.MyIdentity;

namespace CloudDebugger.Features.Credentials;

public class ViewLogModel
{
    public List<AzureIdentityLogEntry>? Log { get; set; }
}