using Azure.Core;
using Azure.MyIdentity;
using Flurl;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CloudDebugger.Features.DefaultAzureCredentials;

public class DefaultAzureCredentialsController : Controller
{
    private readonly ILogger<DefaultAzureCredentialsController> _logger;

    // The double slash is intentional for public cloud. Use "https://graph.microsoft.com/.default" for the Graph API.
    private static readonly string[] scopes = ["https://management.azure.com//.default"];

    public DefaultAzureCredentialsController(ILogger<DefaultAzureCredentialsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }


    /// <summary>
    /// Get an access token using the custom MyDefaultAzureCredential class.
    /// </summary>
    /// <returns></returns>
    public IActionResult GetAccessToken()
    {
        var model = new GetAccessTokenModel();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        try
        {
            model.MyDefaultAzureCredential = CreateMyDefaultAzureCredentialInstance();

            model.AccessToken = GetAccessToken(model.MyDefaultAzureCredential);
            if (model.AccessToken.Token != null)
            {
                model.UrlToJWTIOSite = new Uri("https://jwt.io").SetFragment("value=" + model.AccessToken.Token);
                model.UrlToJWTMSSite = new Uri("https://jwt.ms").SetFragment("access_token=" + model.AccessToken.Token);
            }

            // Using a customization we have made to the MyDefaultAzureCredential class,
            // that exploses the choosen TokenCredential
            model.SelectedTokenCredential = model.MyDefaultAzureCredential.SelectedTokenCredential;

            // Using a customization we have made to the MyDefaultAzureCredential class,
            // that exposes an internal log of what happens under the hood inside MyDefaultAzureCredential
            model.MyDefaultAzureCredentialLog = DefaultAzureCredential.LogText.ToString();

            // Using a customization we have made to the MyDefaultAzureCredential class,
            // that exposes the active token credentials used.
            model.CredentialSources = model.MyDefaultAzureCredential._sources.ToList();

            model.Scopes = string.Join(',', scopes);
        }
        catch (Exception exc)
        {
            model.ErrorMessage = $"Exception: {exc.Message}";
            _logger.LogInformation(exc, "Failed to retrieve access token");
        }

        totalTimeSw.Stop();
        model.ExecutionTime = $"{(int)totalTimeSw.Elapsed.TotalMilliseconds} ms";

        return View(model);
    }


    private static AccessToken GetAccessToken(DefaultAzureCredential credential)
    {
        var tokenRequestContext = new TokenRequestContext(scopes);

        var token = credential.GetToken(tokenRequestContext);

        return token;
    }

    private static DefaultAzureCredential CreateMyDefaultAzureCredentialInstance()
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
