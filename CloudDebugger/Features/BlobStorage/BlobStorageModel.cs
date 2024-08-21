namespace CloudDebugger.Features.BlobStorage;

public class BlobStorageModel
{


    /// <summary>
    /// Storage account SAS token (optional), if not provided, it will authenticate using DefaultAzureCredential 
    /// </summary>
    public string? SASToken { get; set; } = "sp=racwdl&st=2024-08-21T12:19:29Z&se=2024-08-21T20:19:29Z&sv=2022-11-02&sr=c&sig=Hwk2soSd8SjjcuidCX%2Fp%2BBgOWL5dCDmEY3vr4dHk5dc%3D";

    public string? StorageAccountName { get; set; } = "clouddebuggerstorage";       //Must be lowercase
    public string? ContainerName { get; set; } = "clouddebugger";                   //Must be lowercase
    public string? BlobName { get; set; } = "MyBlob.txt";


    public List<(string name, string size)> ContainerContent = new();
    public string? FileContent { get; set; } = "Current time is " + DateTime.Now;


    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}
