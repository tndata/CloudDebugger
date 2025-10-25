// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Core.Pipeline;
using System;
using System.Threading.Tasks;

namespace Azure.MyIdentity
{
    internal class DefaultAzureCredentialImdsRetryPolicy : RetryPolicy
    {
        private static ManagedIdentityResponseClassifier classifier = new ManagedIdentityResponseClassifier();

        public DefaultAzureCredentialImdsRetryPolicy(RetryOptions retryOptions, DelayStrategy delayStrategy = null) : base(retryOptions.MaxRetries,
                    delayStrategy ?? new ImdsRetryDelayStrategy(retryOptions.Delay, retryOptions.MaxDelay))
        { }

        protected override bool ShouldRetry(HttpMessage message, Exception exception)
        {
            message.ResponseClassifier = classifier;
            // For IMDS requests, do not retry until we have observed the first response cycle
            return !ImdsManagedIdentityProbeSource.IsProbRequest(message) ? base.ShouldRetry(message, exception) : false;
        }

        protected override ValueTask<bool> ShouldRetryAsync(HttpMessage message, Exception exception)
        {
            message.ResponseClassifier = classifier;
            // For IMDS requests, do not retry until we have observed the first response cycle
            return !ImdsManagedIdentityProbeSource.IsProbRequest(message) ? base.ShouldRetryAsync(message, exception) : default;
        }

        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            base.Process(message, pipeline);
        }

        public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            var result = base.ProcessAsync(message, pipeline);
            return result;
        }
    }
}
