using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.MyIdentity;
using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.EventGrid
{
    /// <summary>
    /// Azure Event Grid client library for .NET
    /// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventgrid-readme?view=azure-dotnet
    /// 
    /// </summary>
    public class EventGridController : Controller
    {
        private readonly ILogger<EventGridController> _logger;

        private static string _accessKey = "";

        public EventGridController(ILogger<EventGridController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/EventGrid/SendEvents")]
        public IActionResult GetSendEvents(SendEventGridModel model)
        {
            if (model == null)
            {
                model = new SendEventGridModel();
            }
            model.AccessKey = _accessKey;

            return View("SendEvents", model);
        }

        [HttpPost("/EventGrid/SendEvents")]
        public IActionResult PostSendEvents(SendEventGridModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                _accessKey = model.AccessKey ?? "";   //Remember access key
                model.Exception = "";
                model.Message = "";
                string userAssignedClientID = "";

                try
                {
                    EventGridPublisherClient? client = null;
                    if (string.IsNullOrWhiteSpace(_accessKey))
                    {
                        var defaultCredentialOptions = new DefaultAzureCredentialOptions();

                        // See https://github.com/microsoft/azure-container-apps/issues/442
                        // because one or more UserAssignedIdentity can be assigned to an Azure Resource, we have to be explicit about which one to use.
                        userAssignedClientID = Environment.GetEnvironmentVariable("USERASSIGNEDCLIENTID") ?? "";
                        if (string.IsNullOrWhiteSpace(userAssignedClientID) == false)
                        {
                            defaultCredentialOptions.ManagedIdentityClientId = userAssignedClientID;
                        }

                        client = new EventGridPublisherClient(new Uri(model.TopicEndpoint ?? ""),
                                                              new MyDefaultAzureCredential(defaultCredentialOptions));
                    }
                    else
                    {
                        client = new EventGridPublisherClient(new Uri(model.TopicEndpoint ?? ""),
                                                              new AzureKeyCredential(_accessKey));
                    }

                    int id = model.StartNumber;
                    int eventId = model.StartNumber;

                    CloudEvent? @event = null;

                    for (int i = 0; i < model.NumberOfEvents; i++)
                    {
                        if (eventId % 2 == 0)
                        {
                            var order = new OrderEvent()
                            {
                                OrderId = eventId,
                                CustomerName = "Customer " + eventId
                            };

                            @event = new CloudEvent(source: "CloudDebugger", type: "BusinessEvent.NewOrder", jsonSerializableData: order);
                            @event.Subject = "Order" + order.OrderId;

                            Console.WriteLine(@event.Subject);

                        }
                        else
                        {
                            var product = new ProductEvent()
                            {
                                ProductId = eventId,
                                ProductName = "Product " + eventId
                            };

                            @event = new CloudEvent(source: "CloudDebugger", type: "BusinessEvent.NewOrder", jsonSerializableData: product);
                            @event.Subject = "Product" + product.ProductId;

                            _logger.LogInformation("Sendt event to Event Grid:" + @event.Subject);
                        }


                        // Send the event
                        client.SendEventAsync(@event).Wait();

                        eventId++;
                    }

                    model.StartNumber = eventId;
                    model.Message = "Events sent!";

                    if (string.IsNullOrWhiteSpace(userAssignedClientID) == false)
                    {
                        model.Message = model.Message + " Using Entra ID identity " + userAssignedClientID;
                    }
                }
                catch (Exception exc)
                {
                    string msg = "";
                    if (string.IsNullOrWhiteSpace(userAssignedClientID) == false)
                    {
                        msg = "Using Entra ID identity " + userAssignedClientID + "\r\n";
                    }

                    model.Exception = msg + exc.ToString();
                }
            }

            return View("SendEvents", model);
        }
    }


    public class OrderEvent
    {
        public int OrderId { get; set; }
        public string? CustomerName { get; set; }
    }
    public class ProductEvent
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
    }
}
