using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using CloudDebugger.Features.EventHubProduce.Events;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CloudDebugger.Features.EventHubProduce;

/// <summary>
/// Azure Event Hub client library for .NET
/// https://learn.microsoft.com/en-us/azure/event-hubs/sdks
/// 
/// Quickstart: Send events to and receive events from Azure Event Hubs using .NET
/// https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-dotnet-standard-getstarted-send
/// </summary>
public class EventHubProduceController : Controller
{
    private readonly ILogger<EventHubProduceController> _logger;
    private const string connectionSessionKey = "EventHubConnectionString";

    public EventHubProduceController(ILogger<EventHubProduceController> logger)
    {
        _logger = logger;
    }


    [HttpGet]
    public IActionResult ProduceEvents()
    {
        var model = new ProduceEventHubModel()
        {
            ConnectionString = HttpContext.Session.GetString(connectionSessionKey)
        };

        return View(model);
    }

    /// <summary>
    /// Send a number of events to the EventHub
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> ProduceEvents(ProduceEventHubModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model == null)
            return View(new ProduceEventHubModel());

        var ConnectionString = model.ConnectionString ?? "";

        //Remember ConnectionString
        HttpContext.Session.SetString(connectionSessionKey, ConnectionString);

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        try
        {
            var producerClient = GetProducerClient(ConnectionString);

            int eventId = model.StartNumber;

            for (int i = 0; i < model.NumberOfEvents; i++)
            {
                var (eventData, partitionKey) = CreateEvent(eventId);

                await SendEvent(producerClient, eventData, partitionKey: partitionKey);

                eventId++;
            }

            model.StartNumber = eventId;
            model.Message = "Events sent!";
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;

            _logger.LogError(exc, "Failure when sending events to EventHub");
        }

        return View(model);
    }

    private static EventHubProducerClient GetProducerClient(string ConnectionString)
    {
        var client = new EventHubProducerClient(ConnectionString);

        return client;
    }

    /// <summary>
    /// We send one event per batch, so we can provide different partition keys for each event.
    /// </summary>
    private static async Task SendEvent(EventHubProducerClient producerClient, EventData eventData, string partitionKey)
    {
        var batchOptions = new CreateBatchOptions()
        {
            PartitionKey = partitionKey
        };

        using (EventDataBatch eventBatch = await producerClient.CreateBatchAsync(batchOptions))
        {
            eventBatch.TryAdd(eventData);

            await producerClient.SendAsync(eventBatch);
        }
    }


    /// <summary>
    /// Generate sample events
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    private static (EventData eventData, string partitionKey) CreateEvent(int eventId)
    {
        if (eventId % 2 == 0)
        {
            var @event = new EventHubOrderEvent()
            {
                OrderId = eventId,
                CustomerName = "Customer " + eventId
            };

            var eventData = new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event)));

            return (eventData, partitionKey: "order");
        }
        else
        {
            var @event = new EventHubProductEvent()
            {
                ProductId = eventId,
                ProductName = "Product " + eventId
            };

            var eventData = new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event)));

            return (eventData, partitionKey: "product");
        }
    }
}
