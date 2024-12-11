using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.BlobStorageAccessTiers;

public class BlobStorageAccessTiersModel
{
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
    /// Optional SAS token
    /// </summary>
    public string? SASToken { get; set; }

    /// <summary>
    /// The list of all versions of a given blob
    /// </summary>
    public List<BlobDetails>? Blobs { get; set; } = [];

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// How did we authenticate to Azure
    /// </summary>
    public string? AuthenticationMessage { get; set; }
}
