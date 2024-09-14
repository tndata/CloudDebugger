using Azure.Core;

namespace CloudDebugger.Features.TokenCredentailsExplorer;

public class TokenCredentailsExplorerModel
{
    /// <summary>
    /// The received access token.
    /// </summary>
    public AccessToken AccessToken { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

    public List<string>? Log { get; set; }

    public string? ExecutionTime { get; set; }

    /// <summary>
    /// A link to jwt.io including the access token.
    /// </summary>
    public string? UrlToJWTIOSite { get; set; }

    /// <summary>
    /// A link to jwt.ms including the access token.
    /// </summary>
    public string? UrlToJWTMSSite { get; set; }

}