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
    public async Task<IActionResult> SendData(LoadTestingModel model)
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
                    string result = await SendRequests(model.TargetURL, model.TotalNumberOfRequests.Value, model.NumberOfConcurrentRequests.Value);
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

    private static async Task<string> SendRequests(string targetURL, int totalNumberOfRequests, int numberOfConcurrentRequests)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalNumberOfRequests);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfConcurrentRequests);
        int sent = 0, successful = 0, failed = 0, nextRequest = 0;
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds) };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTimeoutSeconds));
        async Task RunWorker()
        {
            while (!cts.IsCancellationRequested)
            {
                if (Interlocked.Increment(ref nextRequest) > totalNumberOfRequests)
                    break;
                var url = new Uri(targetURL.Replace("$ID", Random.Shared.Next(1, 101).ToString()));
                if (cts.IsCancellationRequested)
                    break;
                Interlocked.Increment(ref sent);
                try
                {
                    using var response = await client.GetAsync(url, cts.Token);
                    if (response.IsSuccessStatusCode)
                        Interlocked.Increment(ref successful);
                    else
                        Interlocked.Increment(ref failed);
                }
                catch
                {
                    Interlocked.Increment(ref failed);
                }
            }
        }
        await Task.WhenAll(Enumerable.Range(0, Math.Min(numberOfConcurrentRequests, totalNumberOfRequests)).Select(_ => RunWorker()));
        return $"Total Requests Sent: {sent}, Successful Requests: {successful}, Failed Requests: {failed}";
    }
}
