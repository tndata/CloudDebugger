using Microsoft.AspNetCore.Mvc;

namespace CloudDebugger.Features.LoadTesting;

/// <summary>
/// Controller for load testing, allowing concurrent requests to be sent to a specified destination.
/// </summary>
public class LoadTestingController : Controller
{
    public LoadTestingController()
    {
    }

    public IActionResult SendData()
    {
        var model = new LoadTestingModel()
        {
            TotalNumberOfRequests = 1000,
            NumberOfConcurrentRequests = 20,
            TargetURL = "https://[Enter Domain]/api/customersSlow/$ID"
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult SendData(LoadTestingModel model)
    {
        if (ModelState.IsValid && model != null)
        {
            try
            {
                ArgumentNullException.ThrowIfNullOrEmpty(model.TargetURL);

                model.Exception = "";
                model.Message = "";

                // Ensure the nullable integers have values before passing them
                if (model.TotalNumberOfRequests.HasValue && model.NumberOfConcurrentRequests.HasValue)
                {
                    string result = SendRequests(model.TargetURL, model.TotalNumberOfRequests.Value, model.NumberOfConcurrentRequests.Value);
                    model.Message = result;
                }
                else
                {
                    model.Exception = "TotalNumberOfRequests or NumberOfConcurrentRequests is null.";
                }
            }
            catch (Exception exc)
            {
                string msg = "";
                model.Exception = msg + exc.ToString();
            }
        }

        return View(model);
    }

    private const int RequestTimeoutSeconds = 5;
    private const int CancellationTimeoutSeconds = 180;

    private static string SendRequests(string targetURL, int totalNumberOfRequests, int numberOfConcurrentRequests)
    {
        int totalNumberOfRequestsSent = 0;
        int totalNumberOfSuccessFullRequests = 0;
        int totalNumberOfFailedRequests = 0;
        var random = new Random();

        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
            List<Task> tasks = [];
            using (CancellationTokenSource cts = new(TimeSpan.FromSeconds(CancellationTimeoutSeconds)))
            {
                int requestsPerTask = totalNumberOfRequests / numberOfConcurrentRequests;

                // Ensure the thread pool has enough threads
                ThreadPool.SetMinThreads(numberOfConcurrentRequests, numberOfConcurrentRequests);

                for (int i = 0; i < numberOfConcurrentRequests; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        for (int j = 0; j < requestsPerTask; j++)
                        {
                            try
                            {
                                string targetUrlWithId = targetURL.Replace("$ID", random.Next(1, 101).ToString()); //(1 - 100)
                                HttpResponseMessage response = await client.GetAsync(new Uri(targetUrlWithId), cts.Token);

                                if (response.IsSuccessStatusCode)
                                {
                                    Interlocked.Increment(ref totalNumberOfSuccessFullRequests);
                                }
                                else
                                {
                                    Interlocked.Increment(ref totalNumberOfFailedRequests);
                                }
                            }
                            catch
                            {
                                Interlocked.Increment(ref totalNumberOfFailedRequests);
                            }
                            finally
                            {
                                Interlocked.Increment(ref totalNumberOfRequestsSent);
                            }
                        }
                    }, cts.Token));
                }

                Task.WaitAll(tasks.ToArray());
            }
        }

        return $"Total Requests Sent: {totalNumberOfRequestsSent}, Successful Requests: {totalNumberOfSuccessFullRequests}, Failed Requests: {totalNumberOfFailedRequests}";
    }
}
