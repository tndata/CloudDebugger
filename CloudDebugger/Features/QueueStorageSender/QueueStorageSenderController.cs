using Azure.Storage.Queues;
using CloudDebugger.SharedCode.QueueStorage;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.QueueStorageSender;

/// <summary>
/// Azure Queue Storage Sender tool
/// 
/// This tool will send a number of messages to an Azure Queue Storage. It supposed authentication using SAS Token or Managed Identity.
/// 
/// Resources: 
/// * Getting Started with Azure Queue Service in .NET
///   https://github.com/Azure-Samples/storage-queue-dotnet-getting-started
/// * Azure Storage client libraries for .NET
///   https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage
/// </summary>
public class QueueStorageSenderController : Controller
{

    // Keys for the Session Storage
    private const string QueueUrlSessionKey = "QueueStorageUrlString";
    private const string queueSasTokenSessionKey = "QueueStorageSasTokenString";

    private const string authenticationApproach = "authenticationApproach";

    public QueueStorageSenderController()
    {
    }

    [HttpGet]
    public IActionResult SendMessages()
    {
        var model = new SendMessagesModel()
        {
            SasToken = HttpContext.Session.GetString(queueSasTokenSessionKey) ?? "",
            QueueUrl = HttpContext.Session.GetString(QueueUrlSessionKey) ?? "",

            MessageToSend = """
            {
              "OrderId": ##ID##,
              "CustomerId": 67890,
              "OrderDate": "2023-10-01T14:30:00Z",
              "Items": [
                {
                  "ProductId": 987,
                  "ProductName": "Wireless Mouse",
                  "Quantity": 1,
                  "Price": 25.99
                },
                {
                  "ProductId": 654,
                  "ProductName": "Mechanical Keyboard",
                  "Quantity": 1,
                  "Price": 79.99
                }
              ],
              "TotalAmount": 105.98,
              "Status": "Pending"
            }    
            """
        };

        return View(model);
    }

    /// <summary>
    /// Send a number of events to the EventHub
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> SendMessages(SendMessagesModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model == null)
            return View(new SendMessagesModel());

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        ViewData[authenticationApproach] = "";

        string sasToken = model.SasToken ?? "";
        string queueUrl = model.QueueUrl ?? "";

        //Remember these values in the Session
        HttpContext.Session.SetString(queueSasTokenSessionKey, sasToken);
        HttpContext.Session.SetString(QueueUrlSessionKey, queueUrl);

        try
        {
            (QueueClient? client, string message) = QueueStorageClientBuilder.CreateQueueClient(new Uri(queueUrl), sasToken);
            ViewData[authenticationApproach] = message;

            await SendMessages(client, model);
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
        }

        return View(model);
    }

    /// <summary>
    /// Send messages to the queue
    /// </summary>
    /// <param name="client"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    private static async Task SendMessages(QueueClient client, SendMessagesModel model)
    {
        int eventId = model.StartNumber;

        for (int i = 0; i < model.NumberOfMessages; i++)
        {
            var message = model.MessageToSend ?? "";
            message = message.Replace("##ID##", eventId.ToString());

            await client.SendMessageAsync(message);

            eventId++;
        }

        model.StartNumber = eventId;
        model.Message = $"{model.NumberOfMessages} messages sent!";
    }
}
