using Azure.Core;
using Azure.MyIdentity;

namespace CloudDebugger.Features.Credentials;

public class GetAccessTokenModel
{
    public TokenCredential? SelectedTokenCredential { get; set; }
    public TokenCredential[]? CredentialSources { get; set; }
    public MyDefaultAzureCredential? MyDefaultAzureCredential { get; set; }


    public AccessToken AccessToken { get; set; }
    public string? UrlToJWTIOSite { get; set; }

    public string? Log { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public string? ExecutionTime { get; set; }
    public string? Scopes { get; set; }
}