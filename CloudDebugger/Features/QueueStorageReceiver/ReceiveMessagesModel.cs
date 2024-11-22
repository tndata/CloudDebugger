using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.QueueStorageReceiver;

public class ReceiveMessagesModel
{
    /// <summary>
    /// The URL to the queue storage
    /// </summary>
    [Required]
    public string? QueueUrl { get; set; } = "";

    /// <summary>
    /// Optional SAS access token
    /// </summary>
    public string? SasToken { get; set; } = "";

    /// <summary>
    /// True if we should delete the messages from the queue after reading
    /// </summary>
    public bool DeleteMessagesAfterRead { get; set; } = true;

    /// <summary>
    /// The list of received messages
    /// </summary>
    public List<string>? ReceivedMessages { get; set; }


    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

}