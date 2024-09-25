using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.BlobStorage;

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
    /// Blob Name
    /// </summary>
    [Required]
    public string? BlobName { get; set; }

    public List<(string name, string size)> ContainerContent { get; set; } = [];

    public string? FileContent { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}
