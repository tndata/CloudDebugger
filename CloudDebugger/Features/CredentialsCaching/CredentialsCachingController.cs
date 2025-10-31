using Azure.Core;
using CloudDebugger.SharedCode.Credentials;
using Flurl;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace CloudDebugger.Features.CredentialsCaching;

/// <summary>
//
/// </summary>
public partial class CredentialsCachingController : Controller
{
    private readonly ILogger<CredentialsCachingController> _logger;

    private static readonly string[] testScopes = ["https://management.azure.com//.default"];

    // We use this scope to warm up the credential to avoid caching effects of the main scope
    private static readonly string[] warmUpScopes = ["https://graph.microsoft.com/.default"];

    public CredentialsCachingController(ILogger<CredentialsCachingController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index(int credentialId = 0, string? clientId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (credentialId == 0 && clientId == null)
        {
            clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        }

        var model = new CredentialCachingModel()
        {
            CurrentCredentialId = credentialId,
            ClientId = clientId
        };

        var result = new List<string>();
        CredentialResult? cred = null;
        try
        {
            cred = CredentialsFactory.CreateTokenCredentialInstance(credentialId, clientId);

            if (cred != null && cred.Credential != null)
            {
                model.CredentialMessage = cred.Message;
                model.CredentialName = cred.Credential.GetType().Name;

                var log1 = await TestSingleInstance(credentialId, clientId);
                result.Add(log1);

                var log2 = await TestMultipleInstances(credentialId, clientId);
                result.Add(log2);

                // Get sample access token
                var sampleCred = CredentialsFactory.CreateTokenCredentialInstance(credentialId, clientId);
                if (sampleCred != null)
                {
                    model.AccessToken = await GetAccessTokenAsync(sampleCred.Credential, testScopes);
                    if (model.AccessToken.Token != null)
                    {
                        model.UrlToJWTIOSite = new Uri("https://jwt.io").SetFragment("value=" + model.AccessToken.Token);
                        model.UrlToJWTMSSite = new Uri("https://jwt.ms").SetFragment("access_token=" + model.AccessToken.Token);
                    }
                }
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

        model.Log = result;

        return View(model);
    }

    private static async Task<string> TestSingleInstance(int credentialId, string? clientId)
    {
        StringBuilder sb = new();



        var credential = CredentialsFactory.CreateTokenCredentialInstance(credentialId, clientId);

        if (credential == null || credential.Credential == null)
        {
            return "Failed to create credential instance. credentialId=" + credentialId;
        }

        var credName = credential.Credential.GetType().Name;

        sb.AppendLine($"Test 1: Using the Same {credName} Instance");
        sb.AppendLine($"------------------------------------------------------");
        sb.AppendLine($"var credential = new {credName}(...);");
        sb.AppendLine($"for (int i = 0; i < 10; i++)");
        sb.AppendLine("{");
        sb.AppendLine($"    token = credential.GetToken(...);");
        sb.AppendLine("}");
        sb.AppendLine();

        var tokenCredential = credential.Credential;

        // Warm up with different scope to avoid caching the target scope
        await GetAccessTokenAsync(tokenCredential, warmUpScopes);

        for (int i = 0; i < 10; i++)
        {
            var sw = Stopwatch.StartNew();

            var token = await GetAccessTokenAsync(tokenCredential, testScopes);
            var hash = token.Token.GetHashCode();

            sw.Stop();

            sb.AppendLine($"Attempt {i + 1,2}: {sw.ElapsedMilliseconds,4}ms - Token hash: {hash,12}");
        }

        return sb.ToString();
    }


    private static async Task<string> TestMultipleInstances(int credentialId, string? clientId)
    {
        StringBuilder sb = new();

        var warmupCredential = CredentialsFactory.CreateTokenCredentialInstance(credentialId, clientId);

        if (warmupCredential == null || warmupCredential.Credential == null)
        {
            return "Failed to create credential instance. credentialId=" + credentialId;
        }

        var credName = warmupCredential.Credential.GetType().Name;

        sb.AppendLine($"Test 2: Creating New {credName} Instance Each Time");
        sb.AppendLine($"--------------------------------------------------------------");
        sb.AppendLine($"for (int i = 0; i < 10; i++)");
        sb.AppendLine("{");
        sb.AppendLine($"    var credential = new {credName}(...);");
        sb.AppendLine($"    token = credential.GetToken(...);");
        sb.AppendLine("}");
        sb.AppendLine();

        await GetAccessTokenAsync(warmupCredential.Credential, warmUpScopes);

        for (int i = 0; i < 10; i++)
        {
            var sw = Stopwatch.StartNew();

            var credential = CredentialsFactory.CreateTokenCredentialInstance(credentialId, clientId);

            var token = await GetAccessTokenAsync(credential?.Credential, testScopes);
            var hash = token.Token.GetHashCode();

            sw.Stop();

            sb.AppendLine($"Attempt {i + 1,2}: {sw.ElapsedMilliseconds,4}ms - Token hash: {hash,12}");
        }

        return sb.ToString();
    }

    private static async Task<AccessToken> GetAccessTokenAsync(TokenCredential? credential, string[] scopes)
    {
        if (credential != null)
        {
            var tokenRequestContext = new TokenRequestContext(scopes);
            return await credential.GetTokenAsync(tokenRequestContext, default);
        }
        else
        {
            return new AccessToken("", DateTimeOffset.MinValue);
        }
    }
}