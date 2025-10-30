using Azure.Core;
using CloudDebugger.SharedCode.Credentials;
using Flurl;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CloudDebugger.Features.TokenCredentialsExplorer;

public class TokenCredentialsExplorerController : Controller
{
    private readonly ILogger<TokenCredentialsExplorerController> _logger;

    private static readonly string[] scopes = ["https://management.azure.com//.default"];

    public TokenCredentialsExplorerController(ILogger<TokenCredentialsExplorerController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(int credentialId = 0, string? clientId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (credentialId == 0 && clientId == null)
        {
            // Get the client ID if this is the first request to the page.
            clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        }

        var model = new TokenCredentialsExplorerModel()
        {
            CurrentCredentiald = credentialId,
            ClientId = clientId
        };

        var result = new List<string>();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        CredentialResult? cred = null;
        try
        {
            cred = CredentialsFactory.CreateTokenCredentialInstance(credentialId, clientId);

            if (cred != null && cred.Credential != null)
            {
                model.CredentialMessage = cred.Message;
                model.CredentialName = cred.Credential.GetType().Name;

                model.AccessToken = GetAccessToken(cred.Credential);

                if (model.AccessToken.Token != null)
                {
                    model.UrlToJWTIOSite = new Uri("https://jwt.io").SetFragment("value=" + model.AccessToken.Token);
                    model.UrlToJWTMSSite = new Uri("https://jwt.ms").SetFragment("access_token=" + model.AccessToken.Token);
                }

                result.Add(cred.Credential.ToString() ?? "");
            }
            else
            {
                if (cred != null)
                {
                    // Credential creation error
                    var msg = cred.Message ?? "";
                    model.ErrorMessage = $"Failed to create credentials:\r\n{msg}";
                }
            }
        }
        catch (Exception exc)
        {
            if (cred != null)
                result.Add(cred.Credential?.ToString() ?? "");

            model.ErrorMessage = $"Exception:\r\n{exc.Message}";
            _logger.LogError(exc, "Failed to retrieve access token");
        }

        totalTimeSw.Stop();
        result.Add($"\r\nTotal time {(int)totalTimeSw.Elapsed.TotalMilliseconds} ms");

        model.Log = result;

        return View(model);
    }

    private static AccessToken GetAccessToken(TokenCredential credential)
    {
        var tokenRequestContext = new TokenRequestContext(scopes);

        var token = credential.GetToken(tokenRequestContext, default);

        return token;
    }
}

