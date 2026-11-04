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
            return View(model);

        if (model == null)
            return View(new ProduceEventHubModel());

        var connectionString = model.ConnectionString ?? "";

        //Remember ConnectionString
        HttpContext.Session.SetString(connectionSessionKey, connectionString);

        model.Message = "";
        model.ErrorMessage = "";
        ModelState.Clear();

        try
        {
            await using var producerClient = GetProducerClient(connectionString);

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

    private static EventHubProducerClient GetProducerClient(string connectionString)
    {
        var client = new EventHubProducerClient(connectionString);

        return client;
    }

    /// <summary>
    /// We send one event per batch, so we can provide different partition keys for each event.
    /// </summary>
    private static async Task SendEvent(EventHubProducerClient producerClient, EventData eventData, string partitionKey)
    {
        // Azure maps each partition key to a partition; the same key stays together while the partition count is unchanged.
        // Different keys can map to the same partition, so "order" and "product" do not guarantee separate columns.
        // Use PartitionId instead of PartitionKey when explicit routing is required; they cannot be combined.
        var batchOptions = new CreateBatchOptions()
        {
            PartitionKey = partitionKey
        };

        using (EventDataBatch eventBatch = await producerClient.CreateBatchAsync(batchOptions))
        {
            if (!eventBatch.TryAdd(eventData))
                throw new InvalidOperationException("The event is too large to fit in an Event Hub batch.");

            await producerClient.SendAsync(eventBatch);
        }
    }


    /// <summary>
    /// Generate sample events: even IDs use the order key, odd IDs use the product key.
    /// These readable keys are an exercise convention, not partition IDs.
    /// Create the exercise hub with two partitions so the consumer view (partitions 0 and 1) shows all partitions.
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
