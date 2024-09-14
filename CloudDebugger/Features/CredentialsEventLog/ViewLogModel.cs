using Azure.MyIdentity;

namespace CloudDebugger.Features.CredentialsEventLog;

public class ViewLogModel
{
    public List<AzureIdentityLogEntry>? Log { get; set; }
}