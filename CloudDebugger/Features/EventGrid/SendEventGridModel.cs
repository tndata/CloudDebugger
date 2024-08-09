using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.EventGrid
{
    public class SendEventGridModel
    {
        [Required]
        public string? TopicEndpoint { get; set; } = "";
        [Required]
        public int StartNumber { get; set; } = 1;
        [Required]
        public int NumberOfEvents { get; set; } = 10;

        /// <summary>
        /// EventGrid access token (optional), if not provided, it will authenticate using DefaultAzureCredential (managed identity, environment variables, etc.)
        /// </summary>
        public string? AccessKey { get; set; }

        public string? Exception { get; set; }
        public string? Message { get; set; }
    }
}
