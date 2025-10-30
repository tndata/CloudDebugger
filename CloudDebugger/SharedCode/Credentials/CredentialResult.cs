using Azure.Core;

namespace CloudDebugger.SharedCode.Credentials;

public class CredentialResult
{
    public TokenCredential? Credential { get; set; }
    public string? Message { get; set; }
}
