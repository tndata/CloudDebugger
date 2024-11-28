namespace CloudDebugger.Features.BlobVersioning;

public class BlobVersionDetails
{
    public string? Name { get; set; }
    public string? VersionId { get; set; }
    public string? Content { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsLatestVersion { get; set; }
}