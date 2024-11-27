namespace CloudDebugger.Features.BlobStorageEditor;

/// <summary>
/// Details about blob
/// </summary>
public class BlobDetails
{
    public string? BlobType { get; set; }
    public string? ContentType { get; set; }
    public string? CreatedOn { get; set; }
    public string? LastAccessed { get; set; }
    public string? LastModified { get; set; }
    public Dictionary<string, string> MetaData { get; set; } = [];
}
