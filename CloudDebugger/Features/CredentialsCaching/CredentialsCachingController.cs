using Azure.Core;
using Azure.MyIdentity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CloudDebugger.Features.CredentialsCaching;

/// <summary>
/// Using the hacked "DefaultAzureCredential" version in the included MyIdentity library,
/// </summary>
public class CredentialsCachingController : Controller
{
    private readonly ILogger<CredentialsCachingController> _logger;

    private static readonly string[] scopes = ["https://management.azure.com//.default"];

    public CredentialsCachingController(ILogger<CredentialsCachingController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new GetMultipleAccessTokenModel();

        try
        {
            var result1 = GetTokensUsingSingleInstance();
            model.SingleInstanceLog = result1.log;
            model.SingleInstanceAccessToken = result1.AccessToken;


            var result2 = GetTokensUsingMultipleInstances();
            model.MultipleInstancesLog = result2.log;
            model.MultipleInstanceAccessToken = result2.AccessToken;
        }
        catch (Exception exc)
        {
            model.ErrorMessage = $"Exception:\r\n{exc.Message}";
            _logger.LogError(exc, "Failed to retrieve multiple access token");
        }

        return View(model);
    }


    /// <summary>
    /// Call new DefaultAzureCredential() 10 times in a row on the same instance, 
    /// is the token the same? Same execution time?
    /// </summary>
    /// <returns></returns>
    private static (List<string> log, string AccessToken) GetTokensUsingSingleInstance()
    {
        var result = new List<string>();

        //We reuse the same instance
        var cred = CreateDefaultAzureCredentialInstance();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        AccessToken token = new();
        for (int i = 0; i < 10; i++)
        {
            var sw = new Stopwatch();
            sw.Start();

            token = GetAccessToken(cred);
            var hash = token.Token.GetHashCode();

            sw.Stop();
            result.Add($"Attempt {i} took {(int)sw.Elapsed.TotalMilliseconds}ms - Token.Hashcode={hash}");
        }



        totalTimeSw.Stop();
        result.Add("");
        result.Add($"Total time {totalTimeSw.Elapsed.TotalSeconds} sec");

        var selectedCredential = cred.SelectedTokenCredential;
        result.Add("Selected TokenCredential: " + selectedCredential?.GetType().Name);

        return (result, token.Token);
    }


    /// <summary>
    /// Call new DefaultAzureCredential() 10 times in a row a new instance each time, 
    /// is the token the same? Same execution time?
    /// </summary>
    private static (List<string> log, string AccessToken) GetTokensUsingMultipleInstances()
    {
        var result = new List<string>();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        DefaultAzureCredential? cred = null;

        AccessToken token = new();
        for (int i = 0; i < 10; i++)
        {
            var sw = new Stopwatch();
            sw.Start();

            //We create a new instance each time
            cred = CreateDefaultAzureCredentialInstance();
            token = GetAccessToken(cred);
            var hash = token.Token.GetHashCode();

            sw.Stop();
            result.Add($"Attempt {i} took {(int)sw.Elapsed.TotalMilliseconds} ms - Token.Hashcode={hash}");
        }

        totalTimeSw.Stop();
        result.Add("");
        result.Add($"Total time {totalTimeSw.Elapsed.TotalSeconds} sec");

        if (cred != null)
        {
            var selectedCredential = cred.SelectedTokenCredential;
            result.Add("Selected TokenCredential: " + selectedCredential?.GetType().Name);
        }

        return (result, token.Token);
    }


    private static AccessToken GetAccessToken(DefaultAzureCredential credential)
    {
        var tokenRequestContext = new TokenRequestContext(scopes);

        var token = credential.GetToken(tokenRequestContext);

        return token;
    }

    private static DefaultAzureCredential CreateDefaultAzureCredentialInstance()
    {
        var options = new DefaultAzureCredentialOptions
        {
            Diagnostics =
                        {
                            IsLoggingEnabled = true,
                            LoggedHeaderNames = { "*" },
                            LoggedQueryParameters = { "*" },
                            IsAccountIdentifierLoggingEnabled=true,
                            IsLoggingContentEnabled=true,
                            IsDistributedTracingEnabled=true,
                            IsTelemetryEnabled=true
                        },
        };

        var cred = new DefaultAzureCredential(options);

        return cred;
    }
}
