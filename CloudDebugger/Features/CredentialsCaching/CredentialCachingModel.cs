using Azure.Core;

namespace CloudDebugger.Features.CredentialsCaching;

public class CredentialCachingModel
{
    /// <summary>
    /// The log from calling GetToken multiple times on the same instance 
    /// </summary>
    public List<string>? SingleInstanceLog { get; set; }


    /// <summary>
    /// Optional ClientID, to be used for example if you have muliple user-assigned Managed Identities.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Sample access token
    /// </summary>
    public string? SingleInstanceAccessToken { get; set; }

    /// <summary>
    /// The log from calling GetToken multiple times on new instances each time
    /// </summary>
    public List<string>? MultipleInstancesLog { get; set; }

    /// <summary>
    /// Optional message to display next to the selected token credential details
    /// </summary>
    public string? CredentialMessage { get; set; }
    public string? CredentialName { get; set; }
    public int CurrentCredentialId { get; set; }

    public List<string>? Log { get; set; }


    /// <summary>
    /// Sample access token
    /// </summary>
    public string? MultipleInstanceAccessToken { get; set; }

    /// <summary>
    /// The received access token.
    /// </summary>
    public AccessToken AccessToken { get; set; }


    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }


    /// <summary>
    /// A link to jwt.io including the access token.
    /// </summary>
    public string? UrlToJWTIOSite { get; set; }

    /// <summary>
    /// A link to jwt.ms including the access token.
    /// </summary>
    public string? UrlToJWTMSSite { get; set; }

}
