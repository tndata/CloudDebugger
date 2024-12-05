namespace CloudDebugger.Features.BlobVersioning;

/// <summary>
/// Hold all the details about a given blob version
/// </summary>
public class BlobVersionDetails
{
    /// <summary>
    /// The name of the blob.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The version ID of the blob (ie , a timestamp, like '2024-12-04T17:21:27.0608843Z'
    /// </summary>
    public string? VersionId { get; set; }

    /// <summary>
    /// The content of the blob.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Indicates whether the blob is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Indicating whether this is the latest version of the blob or not.
    /// </summary>
    public bool IsLatestVersion { get; set; }
}
