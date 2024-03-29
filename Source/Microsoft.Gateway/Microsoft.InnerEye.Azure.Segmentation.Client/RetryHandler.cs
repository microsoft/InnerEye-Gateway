﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Azure.Segmentation.Client
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The retry handler for retrying HTTP client requests
    /// </summary>
    public class RetryHandler : DelegatingHandler
    {
        /// <summary>
        /// The maximum number of retries.
        /// </summary>
        private const int MaxRetries = 3;

        /// <summary>
        /// The delay between each retry in milliseconds.
        /// </summary>
        private const int RetryDelayInMilliseconds = 500;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner HTTP message handler.</param>
        public RetryHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        /// <summary>
        /// Override for sending a request.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message or an exception.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request = request ?? throw new ArgumentNullException(nameof(request));

            var i = 0;
            HttpResponseMessage httpResponseMessage = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    httpResponseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    // Only retry on unknown exceptions or service unvailable.
                    if (httpResponseMessage.StatusCode != HttpStatusCode.ServiceUnavailable)
                    {
                        return httpResponseMessage;
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Trace.TraceWarning($"Request failed Method: {request.Method}, RequestUri: {request.RequestUri}, with exception {e}");
                }

                if (i >= MaxRetries)
                {
                    return httpResponseMessage;
                }

                i++;
                Trace.TraceWarning($"Retrying Method: {request.Method}, RequestUri: {request.RequestUri}, retry count = {i}");
                await Task.Delay(RetryDelayInMilliseconds, cancellationToken).ConfigureAwait(false);
            }

            throw new OperationCanceledException(cancellationToken);
        }
    }
}