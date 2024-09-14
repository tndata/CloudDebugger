using Azure.Core;
using Azure.MyIdentity;
using Flurl;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CloudDebugger.Features.TokenCredentailsExplorer;

public class TokenCredentailsExplorerController : Controller
{
    private readonly ILogger<TokenCredentailsExplorerController> _logger;

    private static readonly string[] scopes = ["https://management.azure.com//.default"];

    public TokenCredentailsExplorerController(ILogger<TokenCredentailsExplorerController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(int credential = 0)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var model = new TokenCredentailsExplorerModel();
        var result = new List<string>();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        try
        {
            var tokenCredential = CreateTokenCredentialInstance(credential);

            if (tokenCredential != null)
            {
                model.AccessToken = GetAccessToken(tokenCredential);

                if (model.AccessToken.Token != null)
                {
                    model.UrlToJWTIOSite = new Uri("https://jwt.io").SetFragment("value=" + model.AccessToken.Token);
                    model.UrlToJWTMSSite = new Uri("https://jwt.ms").SetFragment("access_token=" + model.AccessToken.Token);
                }

                result.Add(tokenCredential.ToString());
            }

            totalTimeSw.Stop();
            result.Add("");
            result.Add($"Total time {((int)totalTimeSw.Elapsed.TotalMilliseconds)} ms");
        }
        catch (Exception exc)
        {
            model.ErrorMessage = $"Exception:\r\n{exc.Message}";
            _logger.LogError(exc, "Failed to retrieve access token");
        }

        model.Log = result;

        return View(model);
    }

    private static TokenCredential? CreateTokenCredentialInstance(int credential)
    {

        return credential switch
        {
            1 => CreateAuthorizationCodeCredential(),
            2 => CreateAzureCliCredential(),
            3 => CreateAzureDeveloperCliCredential(),
            4 => CreateAzurePowerShellCredential(),
            5 => null,
            6 => CreateClientAssertionCredential(),
            7 => CreateClientCertificateCredential(),
            8 => CreateClientSecretCredential(),
            9 => CreateDefaultAzureCredential(),
            10 => CreateDeviceCodeCredential(),
            11 => CreateEnvironmentCredential(),
            12 => CreateInteractiveBrowserCredential(),
            13 => CreateManagedIdentityCredential(),
            14 => CreateOnBehalfOfCredential(),
            15 => CreateSharedTokenCacheCredential(),
            16 => CreateUsernamePasswordCredential(),
            17 => CreateVisualStudioCodeCredential(),
            18 => CreateVisualStudioCredential(),
            19 => CreateWorkloadIdentityCredential(),
            _ => null
        };

    }

    private static TokenCredential CreateAuthorizationCodeCredential()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Enables authentication to Microsoft Entra ID using Azure CLI to obtain an access token.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static AzureCliCredential CreateAzureCliCredential()
    {

        return new AzureCliCredential(new AzureCliCredentialOptions
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
                        }
        });
    }

    private static AzureDeveloperCliCredential CreateAzureDeveloperCliCredential()
    {
        return new AzureDeveloperCliCredential(new AzureDeveloperCliCredentialOptions
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
                        }
        });
    }

    private static AzurePowerShellCredential CreateAzurePowerShellCredential()
    {
        return new AzurePowerShellCredential(new AzurePowerShellCredentialOptions
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
                        }
        });
    }

    private static TokenCredential CreateClientAssertionCredential()
    {
        throw new NotImplementedException();
    }

    private static TokenCredential CreateClientCertificateCredential()
    {
        throw new NotImplementedException();
    }

    private static ClientSecretCredential CreateClientSecretCredential()
    {
        throw new NotImplementedException();
    }

    private static MyDefaultAzureCredential CreateDefaultAzureCredential()
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

        //We use our custom hacked version for improved insights
        return new MyDefaultAzureCredential(options);
    }

    private static TokenCredential CreateDeviceCodeCredential()
    {
        throw new NotImplementedException();
    }

    private static EnvironmentCredential CreateEnvironmentCredential()
    {
        return new EnvironmentCredential(new EnvironmentCredentialOptions
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
                        }
        });
    }

    /// <summary>
    /// This one only works for non-server side applications (desktop, console, mobile....)
    /// </summary>
    /// <returns></returns>
    private static InteractiveBrowserCredential CreateInteractiveBrowserCredential()
    {
        return new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
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
                        }
        });
    }

    private static ManagedIdentityCredential CreateManagedIdentityCredential()
    {
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

        if (clientId != null)
        {
            return new ManagedIdentityCredential(clientId, new TokenCredentialOptions()
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
                        }
            }
            );
        }
        else
        {
            return new ManagedIdentityCredential();
        }
    }

    private static TokenCredential CreateOnBehalfOfCredential()
    {
        throw new NotImplementedException();
    }

    private static TokenCredential CreateSharedTokenCacheCredential()
    {
        // This mechanism for Visual Studio authentication has been replaced by the VisualStudioCredential.
        throw new NotImplementedException();
    }

    private static TokenCredential CreateUsernamePasswordCredential()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/azure.identity.usernamepasswordcredential?view=azure-dotnet
        throw new NotImplementedException();
    }

    private static VisualStudioCodeCredential CreateVisualStudioCodeCredential()
    {
        return new VisualStudioCodeCredential(new VisualStudioCodeCredentialOptions
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
                        }
        });
    }

    private static VisualStudioCredential CreateVisualStudioCredential()
    {
        return new VisualStudioCredential(new VisualStudioCredentialOptions
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
                        }
        });
    }

    private static WorkloadIdentityCredential CreateWorkloadIdentityCredential()
    {
        return new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions
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
                        }
        });
    }

    private static AccessToken GetAccessToken(TokenCredential credential)
    {
        var tokenRequestContext = new TokenRequestContext(scopes);

        var token = credential.GetToken(tokenRequestContext, default);

        return token;
    }


}
