using Azure.Storage.Blobs.Models;
using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.BlogStorageDelegationSASToken;

public class UserDelegationModel
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
    /// Blob Name
    /// </summary>
    [Required]
    public string? BlobName { get; set; }

    /// <summary>
    /// Generated SAS token
    /// </summary>
    public string? SASToken { get; set; }

    /// <summary>
    /// Calculated blob URL
    /// </summary>
    public string? BlobUrl { get; set; }

    /// <summary>
    /// The delegation key received from Entra ID to sign the SAS token
    /// </summary>
    public UserDelegationKey? DelegationKey { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}
