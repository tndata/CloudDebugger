using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.BlobVersioning;

public class BlobVersioningModel
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
    /// SAS token
    /// </summary>
    public string? SASToken { get; set; }

    /// <summary>
    /// Blob Name
    /// </summary>
    [Required]
    public string? BlobName { get; set; }

    /// <summary>
    /// The list of all versions of a given blob
    /// </summary>
    public List<BlobVersionDetails>? BlobVersions { get; set; } = [];

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// How did we authenticate to Azure
    /// </summary>
    public string? AuthenticationMessage { get; set; }
}
