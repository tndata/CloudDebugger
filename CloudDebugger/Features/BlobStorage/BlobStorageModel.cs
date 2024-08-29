namespace CloudDebugger.Features.BlobStorage;

public class BlobStorageModel
{
    /// <summary>
    /// Storage account SAS token (optional), if not provided, it will authenticate using DefaultAzureCredential 
    /// </summary>
    public string? SASToken { get; set; } = "";

    public string? StorageAccountName { get; set; } = "clouddebuggerstorage";       //Must be lowercase
    public string? ContainerName { get; set; } = "clouddebugger";                   //Must be lowercase
    public string? BlobName { get; set; } = "MyBlob.txt";

    public List<(string name, string size)> ContainerContent { get; set; } = [];

    public string? FileContent { get; set; } = "";

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}
