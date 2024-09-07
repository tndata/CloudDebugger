using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CloudDebugger.Features.EventHub;

/// <summary>
/// Azure Event Hub client library for .NET
/// https://learn.microsoft.com/en-us/azure/event-hubs/sdks
/// 
/// Quickstart: Send events to and receive events from Azure Event Hubs using .NET
/// https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-dotnet-standard-getstarted-send
/// </summary>
public class EventHubController : Controller
{
    private readonly ILogger<EventHubController> _logger;
    private const string sessionKey = "EventHubConnectionString";

    public EventHubController(ILogger<EventHubController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/EventHub/SendEvents")]
    public IActionResult GetSendEvents(SendEventHubModel model)
    {
        _logger.LogInformation("EventHub.GetSendEvents was called");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        model ??= new SendEventHubModel();
        model.ConnectionString = HttpContext.Session.GetString(sessionKey);

        return View("SendEvents", model);
    }


    [HttpPost("/EventHub/SendEvents")]
    public async Task<IActionResult> PostSendEvents(SendEventHubModel model)
    {
        _logger.LogInformation("EventHub.PostSendEvents was called");

        if (model != null && ModelState.IsValid)
        {
            //Remember ConnectionString
            HttpContext.Session.SetString(sessionKey, model.ConnectionString ?? "");

            model.Exception = "";
            model.Message = "";

            var producerClient = new EventHubProducerClient(model.ConnectionString ?? "");

            try
            {
                int eventId = model.StartNumber;
                EventData? eventData = null;

                for (int i = 0; i < model.NumberOfEvents; i++)
                {
                    if (eventId % 2 == 0)
                    {
                        var @event = new EventHubOrderEvent()
                        {
                            OrderId = eventId,
                            CustomerName = "Customer " + eventId
                        };
                        eventData = new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event)));
                        await SendEvent(producerClient, eventData, partitionKey: "order");
                    }
                    else
                    {
                        var @event = new EventHubProductEvent()
                        {
                            ProductId = eventId,
                            ProductName = "Product " + eventId
                        };
                        eventData = new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event)));
                        await SendEvent(producerClient, eventData, partitionKey: "product");
                    }

                    eventId++;
                }

                model.StartNumber = eventId;
                model.Message = "Events sent!";
            }
            catch (Exception exc)
            {
                model.Exception = exc.ToString();
            }
        }

        return View("SendEvents", model);
    }


    /// <summary>
    /// We send one event per batch, so we can provide different partition keys for each event.
    /// </summary>
    private async static Task SendEvent(EventHubProducerClient producerClient, EventData eventData, string partitionKey)
    {
        var batchOptions = new CreateBatchOptions()
        {
            PartitionKey = partitionKey
        };

        using (EventDataBatch eventBatch = await producerClient.CreateBatchAsync(batchOptions))
        {
            eventBatch.TryAdd(eventData);

            producerClient.SendAsync(eventBatch).Wait();
        }
    }


    [HttpGet("/EventHub/ConsumeEvents")]
    public IActionResult GetConsumeEvents(ConsumeEventHubModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        model ??= new ConsumeEventHubModel();
        model.ConnectionString = HttpContext.Session.GetString(sessionKey);

        return View("ConsumeEvents", model);
    }

    [HttpPost("/EventHub/ConsumeEvents")]
    public async Task<IActionResult> PostConsumeEvents(ConsumeEventHubModel model)
    {
        if (model != null && ModelState.IsValid)
        {
            //Remember ConnectionString
            HttpContext.Session.SetString(sessionKey, model.ConnectionString ?? "");

            model.Exception = "";
            model.Message = "";

            try
            {
                await using (var consumerClient = new EventHubConsumerClient(consumerGroup: model.ConsumerGroup,
                                                                connectionString: model.ConnectionString ?? ""))
                {
                    var options = new ReadEventOptions()
                    {
                        MaximumWaitTime = TimeSpan.FromSeconds(1)  //If specified, should there be no events available before this waiting period expires, an empty event will be returned, allowing control to return to the reader that was waiting.
                    };

                    // It is important to note that this method does not guarantee fairness amongst the partitions during iteration;
                    // each of the partitions compete to publish events to be read by the enumerator. Depending on service communication,
                    // there may be a clustering of events per partition and/or there may be a noticeable bias for a given partition or
                    // subset of partitions.
                    // https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.consumer.eventhubconsumerclient.readeventsasync
                    await foreach (PartitionEvent @event in consumerClient.ReadEventsAsync(startReadingAtEarliestEvent: true,
                                                                                readOptions: options))
                    {
                        // Exit when we receive an emtpty event
                        if (@event.Data == null)
                            break;

                        var entry = BuildEvent(@event);
                        model.Events.Add(entry);
                    }
                }
            }
            catch (Exception exc)
            {
                model.Exception = exc.ToString();
            }
        }

        return View("ConsumeEvents", model);
    }

    private static EventHubLogEntry BuildEvent(PartitionEvent @event)
    {
        var entry = new EventHubLogEntry()
        {
            EventDetails = []
        };

        var contentType = @event.Data.ContentType;
        if (!String.IsNullOrWhiteSpace(contentType))
            entry.EventDetails.Add($"ContentType: {contentType}");

        string correlationId = @event.Data.CorrelationId;
        if (!String.IsNullOrWhiteSpace(correlationId))
            entry.EventDetails.Add($"CorrelationId: {correlationId}");

        DateTimeOffset enqueuedTime = @event.Data.EnqueuedTime;
        entry.EventDetails.Add($"EnqueuedTime: {enqueuedTime}");

        string messageId = @event.Data.MessageId;
        if (!String.IsNullOrWhiteSpace(messageId))
            entry.EventDetails.Add($"MessageId: {messageId}");

        string partitionKey = @event.Data.PartitionKey;
        if (!String.IsNullOrWhiteSpace(partitionKey))
            entry.EventDetails.Add($"PartitionKey: {partitionKey}");

        long sequenceNumber = @event.Data.SequenceNumber;
        entry.EventDetails.Add($"SequenceNumber: {sequenceNumber}");

        entry.Offset = @event.Data.Offset;

        var properties = @event.Data.Properties;
        foreach (var property in properties)
        {
            entry.EventDetails.Add($"{property.Key}: {property.Value}");
        }

        var systemProperties = @event.Data.SystemProperties;
        foreach (var property in systemProperties)
        {
            entry.EventDetails.Add($"{property.Key}: {property.Value}");
        }

        entry.PartitionId = @event.Partition.PartitionId;

        entry.Body = @event.Data.EventBody.ToString();

        return entry;
    }
}
public class EventHubProductEvent
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
}
