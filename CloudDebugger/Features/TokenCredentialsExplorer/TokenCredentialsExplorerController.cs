using Azure.Core;
using Azure.MyIdentity;
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

    public IActionResult Index(int credential = 0, string? clientId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (credential == 0 && clientId == null)
        {
            clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        }

        var model = new TokenCredentialsExplorerModel()
        {
            CurrentCredentialIndex = credential,
            ClientId = clientId
        };

        var result = new List<string>();

        var totalTimeSw = new Stopwatch();
        totalTimeSw.Start();

        CredentialResult? cred = null;
        try
        {
            cred = CreateTokenCredentialInstance(credential, clientId);

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

    private static CredentialResult? CreateTokenCredentialInstance(int credential, string? clientId)
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
            9 => CreateDefaultAzureCredential(clientId),
            10 => CreateDeviceCodeCredential(),
            11 => CreateEnvironmentCredential(),
            12 => CreateInteractiveBrowserCredential(),
            13 => CreateManagedIdentityCredential(clientId),
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

    private static CredentialResult CreateDefaultAzureCredential(string? clientId)
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
                        }
        };
        if (!string.IsNullOrEmpty(clientId))
        {
            options.ManagedIdentityClientId = clientId;
        }

        //We use our custom hacked version for improved insights
        var credential = new DefaultAzureCredential(options);

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

    private static CredentialResult CreateManagedIdentityCredential(string? clientId)
    {
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

            return new() { Credential = credential, Message = "This credential works only within Azure. You may need to provide an optional ClientId." };
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

    private sealed class CredentialResult
    {
        public TokenCredential? Credential { get; set; }
        public string? Message { get; set; }
    }
}

