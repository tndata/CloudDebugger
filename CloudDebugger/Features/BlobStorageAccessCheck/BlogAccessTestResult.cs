namespace CloudDebugger.Features.BlobStorageAccessCheck;

public class BlogAccessTestResult
{
    /// <summary>
    /// Resource type (Service, Container, Object)
    /// </summary>
    public string ResourceType { get; set; }

    public string PermissionTested { get; set; }

    public bool Passed { get; set; }

    public string Message { get; set; }
}