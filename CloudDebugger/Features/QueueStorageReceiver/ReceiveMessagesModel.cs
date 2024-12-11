using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.QueueStorageReceiver;

public class ReceiveMessagesModel
{
    /// <summary>
    /// Storage account name (must be lowercase)
    /// </summary>
    [Required]
    public string? StorageAccountName { get; set; }

    /// <summary>
    /// Queue Name (must be lowercase)
    /// </summary>
    [Required]
    public string? QueueName { get; set; }

    /// <summary>
    /// Optional SAS token
    /// </summary>
    public string? SASToken { get; set; }

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