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
        model.CurrentCredentialIndex = credential;

        var result = new List<string>();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        CredentialResult? cred = null;
        try
        {
            cred = CreateTokenCredentialInstance(credential);

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

                result.Add(cred.Credential?.ToString() ?? "");
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
        result.Add($"\r\nTotal time {((int)totalTimeSw.Elapsed.TotalMilliseconds)} ms");

        model.Log = result;

        return View(model);
    }

    private static CredentialResult? CreateTokenCredentialInstance(int credential)
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

    private static CredentialResult CreateAuthorizationCodeCredential()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Enables authentication to Microsoft Entra ID using Azure CLI to obtain an access token.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static CredentialResult CreateAzureCliCredential()
    {

        var credential = new AzureCliCredential(new AzureCliCredentialOptions
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

        return new() { Credential = credential, Message = "" };
    }

    private static CredentialResult CreateAzureDeveloperCliCredential()
    {
        var credential = new AzureDeveloperCliCredential(new AzureDeveloperCliCredentialOptions
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

        return new() { Credential = credential, Message = "" };
    }

    private static CredentialResult CreateAzurePowerShellCredential()
    {
        var credential = new AzurePowerShellCredential(new AzurePowerShellCredentialOptions
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

        return new() { Credential = credential, Message = "" };
    }

    private static CredentialResult CreateClientAssertionCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateClientCertificateCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateClientSecretCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateDefaultAzureCredential()
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
        var credential = new MyDefaultAzureCredential(options);

        return new() { Credential = credential, Message = "" };
    }

    private static CredentialResult CreateDeviceCodeCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateEnvironmentCredential()
    {
        var credential = new EnvironmentCredential(new EnvironmentCredentialOptions
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

        return new() { Credential = credential, Message = "Refer to the documentation for the required environment variables that must be set." };
    }

    /// <summary>
    /// This one only works for non-server side applications (desktop, console, mobile....)
    /// </summary>
    /// <returns></returns>
    private static CredentialResult CreateInteractiveBrowserCredential()
    {
        var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
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

        return new() { Credential = credential, Message = "This credential is designed for non-browser-based applications, such as console, desktop, or mobile apps, and will not work in browser-based applications." };
    }

    private static CredentialResult CreateManagedIdentityCredential()
    {
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

        if (!string.IsNullOrEmpty(clientId))
        {
            // User-assigned managed identity  
            var credential = new ManagedIdentityCredential(clientId, new TokenCredentialOptions()
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

            return new() { Credential = credential, Message = "This credential works only within Azure. For a user-assigned managed identity, the AZURE_CLIENT_ID environment variable must be set with the Client ID. If not provided, the system-assigned managed identity is used." };
        }
        else
        {
            var credential = new ManagedIdentityCredential();
            return new() { Credential = credential, Message = "" };
        }
    }

    private static CredentialResult CreateOnBehalfOfCredential()
    {
        throw new NotImplementedException();
    }

    private static CredentialResult CreateSharedTokenCacheCredential()
    {
        // This mechanism for Visual Studio authentication has been replaced by the VisualStudioCredential.
        throw new NotImplementedException();
    }

    private static CredentialResult CreateUsernamePasswordCredential()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/azure.identity.usernamepasswordcredential?view=azure-dotnet
        throw new NotImplementedException();
    }

    private static CredentialResult CreateVisualStudioCodeCredential()
    {
        var credential = new VisualStudioCodeCredential(new VisualStudioCodeCredentialOptions
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

        return new() { Credential = credential, Message = "" };
    }

    private static CredentialResult CreateVisualStudioCredential()
    {
        var credential = new VisualStudioCredential(new VisualStudioCredentialOptions
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

        return new() { Credential = credential, Message = "" };
    }

    private static CredentialResult CreateWorkloadIdentityCredential()
    {
        var credential = new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions
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

        return new() { Credential = credential, Message = "supports Microsoft Entra Workload ID authentication on Kubernetes and other hosts supporting workload identity." };
    }


    private static AccessToken GetAccessToken(TokenCredential credential)
    {
        var tokenRequestContext = new TokenRequestContext(scopes);

        var token = credential.GetToken(tokenRequestContext, default);

        return token;
    }


    public class CredentialResult
    {
        public TokenCredential? Credential { get; set; }
        public string? Message { get; set; }
    }
}
