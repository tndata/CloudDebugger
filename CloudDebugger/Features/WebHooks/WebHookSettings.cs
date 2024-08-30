namespace CloudDebugger.Features.WebHooks
{
    public static class WebHookSettings
    {
        /// <summary>
        /// If true, the webhook endpoins will return a 500 error.
        /// </summary>
        public static bool WebHookFailureEnabled { get; set; } = false;

        /// <summary>
        /// True if we should hide the headers in the log UI
        /// </summary>
        public static bool HideHeaders { get; set; } = false;

        /// <summary>
        /// True if we should hide the body in the log UI
        /// </summary>
        public static bool HideBody { get; set; } = false;
    }
}
