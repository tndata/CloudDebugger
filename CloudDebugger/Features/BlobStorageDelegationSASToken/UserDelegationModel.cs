using Azure.Storage.Blobs.Models;
using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.BlobStorageDelegationSASToken;

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
    /// The CorrelationId field in BlobSasBuilder lets you attach a custom ID to the SAS token so that its 
    /// usage can be traced in Azure Storage logs and matched with your own audit records.
    /// See https://learn.microsoft.com/en-us/rest/api/storageservices/create-user-delegation-sas
    /// </summary>
    public string? CorrelationId { get; set; }



    /// <summary>
    /// The delegation key received from Entra ID to sign the SAS token
    /// </summary>
    public UserDelegationKey? DelegationKey { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}
