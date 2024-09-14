namespace CloudDebugger.Features.CredentialsCaching;

public class GetMultipleAccessTokenModel
{
    /// <summary>
    /// The log from calling GetToken multiple times on the same instance 
    /// </summary>
    public List<string>? SingleInstanceLog { get; set; }

    /// <summary>
    /// Sample access token
    /// </summary>
    public string SingleInstanceAccessToken { get; set; }

    /// <summary>
    /// The log from calling GetToken multiple times on new instances each time
    /// </summary>
    public List<string>? MultipleInstancesLog { get; set; }

    /// <summary>
    /// Sample access token
    /// </summary>
    public string MultipleInstanceAccessToken { get; set; }


    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}