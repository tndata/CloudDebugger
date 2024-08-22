using Azure.Storage.Blobs.Models;

namespace CloudDebugger.Features.BlobStorage;

public class UserDelegationModel
{
    //Generated SAS token
    public string? SASToken { get; set; } = "";

    public string? StorageAccountName { get; set; } = "clouddebuggerstorage";       //Must be lowercase
    public string? ContainerName { get; set; } = "clouddebugger";                   //Must be lowercase
    public string? BlobName { get; set; } = "MyBlob.txt";

    public UserDelegationKey DelegationKey { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}
