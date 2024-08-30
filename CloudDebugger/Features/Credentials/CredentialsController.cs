using Azure.Core;
using Azure.MyIdentity;
using Flurl;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CloudDebugger.Features.Credentials;

public class CredentialsController : Controller
{
    private readonly ILogger<CredentialsController> _logger;

    // The double slash is intentional for public cloud.
    // private static readonly string[] scopes = ["https://graph.microsoft.com/.default"];
    private static readonly string[] scopes = ["https://management.azure.com//.default"];

    public CredentialsController(ILogger<CredentialsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult GetAccessToken()
    {
        var model = new GetAccessTokenModel();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        try
        {
            model.MyDefaultAzureCredential = CreateMyDefaultAzureCredentialInstance();
            model.AccessToken = GetAccessToken(model.MyDefaultAzureCredential);

            // Customization we have made to the MyDefaultAzureCredential class
            model.SelectedTokenCredential = model.MyDefaultAzureCredential.SelectedTokenCredential;
            model.Log = MyDefaultAzureCredential.LogText.ToString();

            if (model.AccessToken.Token != null)
                model.UrlToJWTIOSite = new Uri("https://jwt.io").SetFragment("value=" + model.AccessToken.Token);

            model.Scopes = string.Join(',', scopes);
        }
        catch (Exception exc)
        {
            string str = $"Exception:\r\n{exc.Message}";
            model.ErrorMessage = str;
            _logger.LogInformation(exc, "Failed to retrieve access token");
        }
        totalTimeSw.Stop();
        model.ExecutionTime = $"{totalTimeSw.Elapsed.TotalMilliseconds} ms";

        return View(model);
    }


    public IActionResult GetMultipleAccessTokens()
    {
        var model = new GetMultipleAccessTokenModel();

        try
        {
            model.SingleInstanceLog = GetTokensUsingSingleInstance();
            model.MultipleInstancesLog = GetTokensUsingMultipleInstances();
        }
        catch (Exception exc)
        {
            model.ErrorMessage = $"Exception:\r\n{exc.Message}";
            _logger.LogError(exc, "Failed to retrieve multiple access token");
        }

        return View(model);
    }


    /// <summary>
    /// Call new MyDefaultAzureCredential() 10 times in a row a new instance each time, 
    /// is the token the same? Same execution time?
    /// </summary>
    private static List<string>? GetTokensUsingMultipleInstances()
    {
        var result = new List<string>();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        AccessToken token = new();
        for (int i = 0; i < 10; i++)
        {
            var sw = new Stopwatch();
            sw.Start();

            //We create a new instance each time
            var cred = CreateMyDefaultAzureCredentialInstance();
            token = GetAccessToken(cred);
            var hash = token.Token.GetHashCode();

            sw.Stop();
            result.Add($"Attempt {i} took {sw.Elapsed.TotalMilliseconds}ms - Token.Hashcode={hash}");
        }

        result.Add("");
        result.Add("Sample access token");
        result.Add(token.Token);

        totalTimeSw.Stop();
        result.Add("");
        result.Add($"Total time {totalTimeSw.Elapsed.TotalSeconds} sec");

        return result;
    }

    /// <summary>
    /// Call new MyDefaultAzureCredential() 10 times in a row on the same instance, 
    /// is the token the same? Same execution time?
    /// </summary>
    /// <returns></returns>
    private static List<string>? GetTokensUsingSingleInstance()
    {
        var result = new List<string>();

        //We reuse the same instance
        var cred = CreateMyDefaultAzureCredentialInstance();

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
            result.Add($"Attempt {i} took {sw.Elapsed.TotalMilliseconds}ms - Token.Hashcode={hash}");
        }

        result.Add("");
        result.Add("Sample access token");
        result.Add(token.Token);

        totalTimeSw.Stop();
        result.Add("");
        result.Add($"Total time {totalTimeSw.Elapsed.TotalSeconds} sec");

        return result;
    }


    public IActionResult ViewLog()
    {
        // Get the internal custom log from the custom MyAzureIdentity library
        var model = new ViewLogModel()
        {
            Log = MyAzureIdentityLog.Log
        };

        return View(model);
    }

    public IActionResult ClearLog()
    {
        MyAzureIdentityLog.ClearLog();
        return RedirectToAction("Index");
    }

    public IActionResult RetryPolicy()
    {
        return View();
    }


    private static AccessToken GetAccessToken(MyDefaultAzureCredential credential)
    {
        var tokenRequestContext = new TokenRequestContext(scopes);

        var token = credential.GetToken(tokenRequestContext);

        return token;
    }

    private static MyDefaultAzureCredential CreateMyDefaultAzureCredentialInstance()
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
                        },
        };

        var cred = new MyDefaultAzureCredential(options);

        return cred;
    }
}
