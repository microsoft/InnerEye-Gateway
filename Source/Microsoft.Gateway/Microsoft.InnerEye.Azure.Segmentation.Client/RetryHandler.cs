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
            var i = 0;
            HttpResponseMessage httpResponseMessage = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    httpResponseMessage = await base.SendAsync(request, cancellationToken);

                    // Only retry on unknown exceptions or service unvailable.
                    if (httpResponseMessage.StatusCode != HttpStatusCode.ServiceUnavailable)
                    {
                        return httpResponseMessage;
                    }
                }
                catch (Exception e)
                {
                    //Trace.TraceWarning($"Request failed Method: {request.Method}, RequestUri: {request.RequestUri}, with exception {e}");
                    Trace.TraceWarning($"Request failed: {request}, with exception {e}");
                }

                if (i >= MaxRetries)
                {
                    return httpResponseMessage;
                }

                i++;
                //Trace.TraceWarning($"Retrying Method: {request.Method}, RequestUri: {request.RequestUri}, retry count = {i}");
                Trace.TraceWarning($"Retrying: {request}, retry count = {i}");

                await Task.Delay(RetryDelayInMilliseconds);
            }

            throw new OperationCanceledException(cancellationToken);
        }
    }
}