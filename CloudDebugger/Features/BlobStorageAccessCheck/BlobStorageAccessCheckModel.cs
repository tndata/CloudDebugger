using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.BlobStorageAccessCheck;

public class BlobStorageAccessCheckModel
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
    public bool AnonymousAccess { get; set; }

    /// <summary>
    /// Blob to delete name
    /// </summary>
    public string? BlobToDeleteName { get; set; }

    /// <summary>
    /// Blob to read and updte name
    /// </summary>
    public string? BlobToReadUpdate { get; set; }

    /// <summary>
    /// How did we try to authenticate?
    /// </summary>
    public string? AuthenticationApproach { get; set; }

    public List<BlogAccessTestResult> TestResults { get; set; } = [];

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}
