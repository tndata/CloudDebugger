namespace CloudDebugger.Features.AppServices;

public class LocalCacheModel
{
    /// <summary>
    /// "True" if the cache is enabled.
    /// </summary>
    public string? LocalCacheEnabled { get; set; }

    /// <summary>
    /// "TRUE" if the cache is ready.
    /// </summary>
    public string? LocalCacheReady { get; set; }

    /// <summary>
    /// Controls if the cache is enabled or not. Set to "Always" to enable. or "Disabled" to disable it.
    /// </summary>
    public string? LocalCacheOption { get; set; }

    /// <summary>
    /// If set, should be a number in MB for the local cache size (1-2000 MB)
    /// </summary>
    public string? LocalCacheSize { get; set; }

    /// <summary>
    /// If set, should be Should either be ReadOnly (Cache is read-only) or WriteButDiscardChanges (Allow writes to local cache but discard changes made locally.)
    /// </summary>
    public string? LocalCacheReadWriteOptions { get; set; }

    /// <summary>
    /// If set, then it should usually be "PrimaryStorageVolume" or "LocalCache"
    /// </summary>
    public string? WebSiteVolumeType { get; set; }
}
