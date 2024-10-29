using System.ComponentModel.DataAnnotations;

namespace CloudDebugger.Features.QueueStorageSender;

public class SendMessagesModel
{
    [Required]
    public string? QueueUrl { get; set; } = "";

    public string? SasToken { get; set; } = "";

    [Required]
    public int StartNumber { get; set; } = 1;
    [Required]
    public int NumberOfMessages { get; set; } = 10;

    [Required]
    public string? MessageToSend { get; set; }


    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}