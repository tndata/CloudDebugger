using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.QueueStorageReceiver;

public class ReceiveMessagesModel
{
    [Required]
    public string? QueueUrl { get; set; } = "";

    public string? SasToken { get; set; } = "";

    public bool DeleteMessagesAfterRead { get; set; } = true;

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

    public List<string>? ReceivedMessages { get; set; }
}