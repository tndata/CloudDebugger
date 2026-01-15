using Azure.Storage.Queues;
using CloudDebugger.SharedCode.QueueStorage;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.QueueStorageSender;

/// <summary>
/// Azure Queue Storage Sender tool
/// 
/// This tool will send a number of messages to an Azure Queue Storage. 
/// It supports authentication using SAS Token or Managed Identity.
/// 
/// Resources: 
/// * Documentation for this tool
///   https://github.com/tndata/CloudDebugger/wiki/QueueStorage 
/// * Getting Started with Azure Queue Service in .NET
///   https://github.com/Azure-Samples/storage-queue-dotnet-getting-started
/// * Azure Storage client libraries for .NET
///   https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage
/// </summary>
public class QueueStorageSenderController : Controller
{

    // Keys for the Session Storage
    private const string storageAccountSessionKey = "StorageAccount";
    private const string queueNameSessionKey = "QueueNameContainer";
    private const string sasTokenSessionKey = "QueueSasToken";

    private const string authenticationApproach = "authenticationApproach";

    public QueueStorageSenderController()
    {
    }

    [HttpGet]
    public IActionResult SendMessages()
    {
        var model = new SendMessagesModel()
        {
            StorageAccountName = HttpContext.Session.GetString(storageAccountSessionKey) ?? "",
            QueueName = HttpContext.Session.GetString(queueNameSessionKey) ?? "",
            SASToken = HttpContext.Session.GetString(sasTokenSessionKey) ?? "",

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
    /// Send a number of messages to Azure Queue Storage
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

        string storageAccount = model.StorageAccountName?.Trim() ?? "";
        string queueName = model.QueueName?.Trim() ?? "";
        string sasToken = model.SASToken?.Trim() ?? "";

        //Remember these fields in the session
        HttpContext.Session.SetString(storageAccountSessionKey, storageAccount);
        HttpContext.Session.SetString(queueNameSessionKey, queueName);
        HttpContext.Session.SetString(sasTokenSessionKey, sasToken);
        try
        {
            (QueueServiceClient? queueServiceClient, string message) = QueueStorageClientBuilder.CreateQueueServiceClient(storageAccount, sasToken);
            ViewData[authenticationApproach] = message;

            if (queueServiceClient != null)
            {
                var client = queueServiceClient.GetQueueClient(queueName);

                await SendMessages(client, model);
            }
            else
            {
                throw new InvalidOperationException("Could not create a QueueServiceClient");
            }
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

        for (int i = 0; i < model.NumberOfMessagesToSend; i++)
        {
            var message = model.MessageToSend ?? "";
            message = message.Replace("##ID##", eventId.ToString());

            await client.SendMessageAsync(message);

            eventId++;
        }

        model.StartNumber = eventId;
        model.Message = $"{model.NumberOfMessagesToSend} messages sent!";
    }
}
