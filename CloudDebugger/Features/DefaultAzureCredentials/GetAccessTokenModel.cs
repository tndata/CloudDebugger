using Azure.Core;
using Azure.MyIdentity;

namespace CloudDebugger.Features.DefaultAzureCredentials;

public class GetAccessTokenModel
{
    /// <summary>
    /// The TokenCredential that was selected by MyDefaultAzureCredential.
    /// </summary>
    public TokenCredential? SelectedTokenCredential { get; set; }

    /// <summary>
    /// List of TokenCredentials that MyDefaultAzureCredential will try to get token from.
    /// </summary>
    public List<TokenCredential>? CredentialSources { get; set; }

    /// <summary>
    /// MyAzureIdentity.DefaultAzureCredential is the customized version of DefaultAzureCredential that includes various logging and debugging features.
    /// </summary>
    public DefaultAzureCredential? MyDefaultAzureCredential { get; set; }

    /// <summary>
    /// The received access token.
    /// </summary>
    public AccessToken AccessToken { get; set; }

    /// <summary>
    /// A link to jwt.io including the access token.
    /// </summary>
    public string? UrlToJWTIOSite { get; set; }

    /// <summary>
    /// A link to jwt.ms including the access token.
    /// </summary>
    public string? UrlToJWTMSSite { get; set; }

    /// <summary>
    /// This is the log from MyDefaultAzureCredential
    /// </summary>
    public string? MyDefaultAzureCredentialLog { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

    public string? ExecutionTime { get; set; }
    public string? Scopes { get; set; }
}