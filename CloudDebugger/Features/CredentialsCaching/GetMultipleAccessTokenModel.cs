namespace CloudDebugger.Features.CredentialsCaching;

public class GetMultipleAccessTokenModel
{
    public List<string>? SingleInstanceLog { get; set; }
    public List<string>? MultipleInstancesLog { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}