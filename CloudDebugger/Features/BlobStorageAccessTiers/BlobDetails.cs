namespace CloudDebugger.Features.BlobStorageAccessTiers;

/// <summary>
/// Hold all the details about a given blob and its access tiers
/// </summary>
public class BlobDetails
{
    /// <summary>
    /// The name of the blob.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Specifies the blob's current access tier (e.g., Hot, Cool, Cold, or Archive).
    /// </summary>
    public string? AccessTier { get; set; }

    /// <summary>
    /// Helper for sorting purposes. (hot=1, cool=2, cold=3, archive=4)
    /// </summary>
    public int TierOrder { get; set; }

    /// <summary>
    /// Indicates whether the access tier is explicitly 
    /// set or inferred from the storage account.
    /// </summary>
    public string? AccessTierInferred { get; set; }

    /// <summary>
    ///The time the tier was changed on the object. This is only returned if the tier on the block blob was ever set.
    /// </summary>
    public string? AccessTierChangedOn { get; set; }

    /// <summary>
    /// If the blob is in the Archive tier and being rehydrated, this shows the priority level 
    /// (e.g., High or Standard).
    /// </summary>
    public string? RehydratePriority { get; set; }

    /// <summary>
    /// For blob storage LRS accounts, valid values are rehydrate-pending-to-hot/rehydrate-pending-to-cool.
    /// If the blob is being rehydrated and is not complete then this header is returned indicating that
    /// rehydrate is pending and also tells the destination tier.
    /// </summary>
    public string? ArchiveStatus { get; set; }

    /// <summary>
    /// The content of the blob.
    /// </summary>
    public string? Content { get; set; }
}
