using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CloudDebugger.Features.OpenTelemetry;

/// <summary>
/// Controller for handling OpenTelemetry operations.
/// 
/// The Azure Monitor Distro client SDK Documentation
/// https://github.com/Azure/azure-sdk-for-net/tree/Azure.Monitor.OpenTelemetry.AspNetCore_1.2.0/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore 
/// 
/// HttpClient and HttpWebRequest instrumentation for OpenTelemetry
/// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Http
/// 
/// SqlClient Instrumentation for OpenTelemetry
/// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.SqlClient
/// </summary>
public class OpenTelemetryController : Controller
{
    // This ActivitySource must be added to the sources that OpenTelemetry listens to, otherwise activities from this source will be ignored.
    private static readonly ActivitySource activitySource = new("CloudDebugger");

    private static readonly Meter meter = new("CloudDebugger.Counters", "1.0");
    private static readonly Counter<int> simpleCounter = meter.CreateCounter<int>(name: "SimpleCounter", unit: "clicks", description: "A very simple counter");
    private static readonly Counter<int> advancedCounter = meter.CreateCounter<int>(name: "AdvancedCounter", unit: "clicks", description: "A slightly more advanceded counter");

    // The "StandardHistogram" uses the default aggregator/buckets
    private static readonly Histogram<long> standardHistogram = meter.CreateHistogram<long>(
        name: "StandardHistogram",
        unit: "none",
        description: "Sample standard histogram with default boundaries.");

    private static readonly Histogram<long> advancedHistogram = meter.CreateHistogram<long>(
        name: "AdvancedHistogram",
        unit: "none",
        description: "Sample histogram with 10 buckets between 0 and 1000.");

    private static readonly string[] countries = ["Sweden", "Denmark", "Norway", "Finland"];
    private static readonly Random random = new();

    public OpenTelemetryController()
    {
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Counter()
    {
        var model = new CounterModel()
        {
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Counter(CounterModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Message = "";
            model.Exception = "";

            var tags = new KeyValuePair<string, object?>[]
            {
                new("user.id", "user123"),
                new("operation.name", "UserClick")
            };

            simpleCounter.Add(1, tags);

            model.Message = "Simple counter incremented";
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View("Counter", model);
    }

    [HttpPost]
    public IActionResult AdvancedCounter(CounterModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Message = "";
            model.Exception = "";


            string country = GetRandomCountry();
            var tags = new KeyValuePair<string, object?>[]
            {
                new("country",country)
            };

            advancedCounter.Add(1, tags);

            model.Message = $"Advanced counter incremented with country='{country}'";
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View("Counter", model);
    }

    public static string GetRandomCountry()
    {
        int index = random.Next(countries.Length);
        return countries[index];
    }

    public IActionResult Histogram()
    {
        var model = new HistogramModel()
        {
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult StandardHistogram(HistogramModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Message = "";
            model.Exception = "";

            for (int i = 0; i < 10; i++)
            {
                long midpointValue = i * 100 + 50;   // e.g., 50, 150, 250, ...
                int count = (int)Math.Pow(2, i);     // 1,2,4,8,...512

                for (int c = 0; c < count; c++)
                {
                    // Each call to Record is considered one measurement for the aggregator
                    standardHistogram.Record(midpointValue);
                }
            }

            model.Message = "Simple Histogram updated";
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View("Histogram", model);
    }


    [HttpPost]
    public IActionResult AdvancedHistogram(HistogramModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Message = "";
            model.Exception = "";

            for (int i = 0; i < 10; i++)
            {
                long midpointValue = i * 100 + 50;   // e.g., 50, 150, 250, ...
                int count = (int)Math.Pow(2, i);     // 1,2,4,8,...512

                for (int c = 0; c < count; c++)
                {
                    // Each call to Record is considered one measurement for the aggregator
                    advancedHistogram.Record(midpointValue);
                }
            }

            model.Message = "Advanced Histogram updated";
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View("Histogram", model);
    }


    public IActionResult SimpleTrace()
    {
        var model = new OpenTelemetryModel()
        {
            LogMessage = "My simple trace message"
        };

        return View(model);
    }


    /// <summary>
    /// Handles the submission of a simple trace
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult SimpleTrace(OpenTelemetryModel model)
    {
        try
        {
            model.Message = "";
            model.Exception = "";

            if (ModelState.IsValid)
            {
                // Start a new root Activity
                using (var activity = activitySource.StartActivity("SimpleTrace"))
                {
                    if (activity != null)
                    {
                        // Add custom event
                        activity.AddEvent(new ActivityEvent("MyCustomEvent", DateTime.UtcNow, new ActivityTagsCollection
                        {
                            { "event.id", Guid.NewGuid().ToString() },
                            { "event.description", "A custom event occurred" },
                            { "event.message", model.LogMessage },
                            { "event.service", "CloudDebugger" },
                            { "event.severity", "info" }
                        }));

                        // Custom attributes
                        activity.SetTag("custom.today", DateTime.UtcNow.Day);
                        activity.SetTag("custom.month", DateTime.UtcNow.Month);

                        var traceId = Activity.Current?.TraceId.ToString() ?? "[Unknown]";
                        model.Message = $"Activity and custom event sent to OpenTelemetry. TraceID={traceId}";
                    }
                    else
                    {
                        model.Message = "Tracing is not enabled";
                    }
                }
            }
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View(model);
    }

    public IActionResult ComplexTrace()
    {
        var model = new OpenTelemetryModel()
        {
            LogMessage = "My Custom Trace message"
        };

        return View(model);
    }

    /// <summary>
    /// Create a more complex trace
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> ComplexTrace(OpenTelemetryModel model)
    {
        try
        {
            model.Message = "";
            model.Exception = "";

            if (ModelState.IsValid)
            {
                if (activitySource == null)
                {
                    // If activity is null, tracing is not enabled.
                    model.Message = "Tracing is not enabled";
                }
                else
                {
                    // Start a new root Activity
                    using (var activity = activitySource.StartActivity("ComplexTrace"))
                    {
                        await DoWork1();
                        await DoWork2();
                        await DoWork3();
                        await DoWork4();
                        await DoWork5(model);
                    }

                    var traceId = Activity.Current?.TraceId.ToString() ?? "[Unknown]";

                    model.Message = $"Complex trace sent to OpenTelemetry. TraceID={traceId}";
                }
            }
        }
        catch (Exception exc)
        {
            model.Exception = exc.ToString();
        }

        return View(model);
    }


    private static async Task DoWork1()
    {
        using (var activity = activitySource.StartActivity(name: "CheckForBannedUsers"))
        {
            await Task.Delay(500);
        }
    }

    private static async Task DoWork2()
    {
        using (var activity = activitySource.StartActivity(name: "CheckForFraud"))
        {
            await Task.Delay(500);
        }
    }

    private static async Task DoWork3()
    {
        using (var activity = activitySource.StartActivity(name: "CheckPoliceRecord"))
        {
            await Task.Delay(500);
        }
    }

    private static async Task DoWork4()
    {
        using (var activity = activitySource.StartActivity(name: "Make API call"))
        {
            if (activity is null)
                return;

            try
            {
                // Simulate work: external call
                //This request will take two seconds to complete
                using (var client = new HttpClient())
                {
                    await client.GetStringAsync(new Uri("https://httpbin.org/delay/2"));
                }
            }
            catch (Exception ex)
            {
                activity.SetTag("error", true);
                activity.SetTag("error.message", ex.Message);
                throw;
            }
        }
    }

    private static async Task DoWork5(OpenTelemetryModel model)
    {
        using (var activity = activitySource.StartActivity(name: "Complex Step (AI Processing)"))
        {
            if (activity is null)
                return;

            // Set tags for the activity (key-value metadata)
            activity.SetTag("operation.id", Guid.NewGuid().ToString());
            activity.SetTag("operation.name", "ComplexTraceOperation");
            activity.SetTag("user.id", "Joe-123");
            activity.SetTag("request.priority", "High");
            activity.SetTag("timestamp.start", DateTime.UtcNow.ToString("o"));

            // Add baggage items (key-value pairs that propagate downstream)
            Baggage.SetBaggage("bag.payment.id", Guid.NewGuid().ToString());
            Baggage.SetBaggage("bag.operation.context", "ComplexTrace");

            // Log an initial event with metadata
            activity.AddEvent(new ActivityEvent("Trace started", tags: new ActivityTagsCollection
                 {
                     { "event.severity", "info" },
                     { "description", "Starting complex trace operation" }
                 }));

            activity.AddEvent(new ActivityEvent("Processing workitem - step 1"));

            await Task.Delay(250);

            activity.AddEvent(new ActivityEvent("Processing workitem - step 2"));

            await Task.Delay(250);

            activity.AddEvent(new ActivityEvent("Processing workitem - step 3"));

            await Task.Delay(500);

            var eventTags = new ActivityTagsCollection(list: new Dictionary<string, object?>
                 {
                     { "eventTags.Step", "3" },
                     { "eventTags.Status", "OK" },
                     { "eventTags.message", model.LogMessage },
                 });

            activity.AddEvent(new ActivityEvent("Processing the result - step 3", tags: eventTags));

            await Task.Delay(500);

            // Log completion of the operation
            activity.AddEvent(new ActivityEvent("Trace completed", tags: new ActivityTagsCollection
                 {
                     { "final.status", "Success" },
                     { "completion.timestamp", DateTime.UtcNow.ToString("o") }
                 }));
        }

    }
}

