namespace CloudDebugger;

public static class GlobalSettings
{
    /// <summary>
    /// True if the Prometheus exporter is enabled. 
    /// When we use Azure Monitor, then this is disabled.
    /// </summary>
    public static bool PrometheusExporterEnabled { get; set; }
}
