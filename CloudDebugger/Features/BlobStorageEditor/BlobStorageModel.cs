using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CloudDebugger.Features.BlobStorageEditor;

public class BlobStorageModel
{
    /// <summary>
    /// Storage account SAS token (optional), if not provided, it will authenticate using DefaultAzureCredential 
    /// </summary>
    public string? SASToken { get; set; }

    /// <summary>
    /// Storage account name (must be lowercase)
    /// </summary>
    [Required]
    public string? StorageAccountName { get; set; }

    /// <summary>
    /// Container Name (must be lowercase)
    /// </summary>
    [Required]
    public string? ContainerName { get; set; }

    /// <summary>
    /// True if we want to access the blob storage anonymously
    /// </summary>
    [Required]
    [JsonRequired]
    public bool AnonymousAccess { get; set; }

    /// <summary>
    /// Blob Path
    /// </summary>
    public string? Path { get; set; } = "";

    /// <summary>
    /// Blob Name
    /// </summary>
    public string? BlobName { get; set; }

    /// <summary>
    /// Blob content
    /// </summary>
    public string? BlobContent { get; set; }


    /// <summary>
    /// List of the blobs in the container
    /// </summary>
    public List<(string name, string size)> ContainerContent { get; set; } = [];

    /// <summary>
    /// Details about the loaded blob
    /// </summary>
    public BlobDetails? Blob { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// How did we authenticate to Azure
    /// </summary>
    public string? AuthenticationMessage { get; set; }
}
