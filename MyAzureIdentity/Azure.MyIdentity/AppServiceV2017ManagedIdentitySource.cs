﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;

namespace Azure.MyIdentity
{
    internal class AppServiceV2017ManagedIdentitySource : AppServiceManagedIdentitySource
    {
        protected override string AppServiceMsiApiVersion => "2017-09-01";
        protected override string SecretHeaderName => "secret";
        protected override string ClientIdHeaderName => "clientid";


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("AppServiceV2017ManagedIdentitySource");
            sb.AppendLine(base.ToString());
            return sb.ToString();
        }


        public static string TryCreateLog = "";


        public static ManagedIdentitySource TryCreate(ManagedIdentityClientOptions options)
        {
            var msiSecret = EnvironmentVariables.MsiSecret;

            var sb = new StringBuilder();
            sb.AppendLine($"AppServiceV2017ManagedIdentitySource");
            sb.AppendLine($" - msiSecret={msiSecret}");
            sb.AppendLine($" - MsiEndpoint={EnvironmentVariables.MsiEndpoint}");
            TryCreateLog = sb.ToString();


            return TryValidateEnvVars(EnvironmentVariables.MsiEndpoint, msiSecret, out Uri endpointUri)
                ? new AppServiceV2017ManagedIdentitySource(options.Pipeline, endpointUri, msiSecret, options)
                : null;
        }

        private AppServiceV2017ManagedIdentitySource(CredentialPipeline pipeline, Uri endpoint, string secret,
            ManagedIdentityClientOptions options) : base(pipeline, endpoint, secret, options)
        {
        }
    }
}
