using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.QueueStorageSender;

public class SendMessagesModel
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
    /// The message number to start counting from 
    /// </summary>
    [Required]
    public int StartNumber { get; set; } = 1;

    /// <summary>
    /// Number of messages to send to the queue
    /// </summary>
    [Required]
    public int NumberOfMessagesToSend { get; set; } = 10;

    /// <summary>
    /// The message to send to the queue
    /// </summary>
    [Required]
    public string? MessageToSend { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}