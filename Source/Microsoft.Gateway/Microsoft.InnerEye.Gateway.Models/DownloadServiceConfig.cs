// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Additional configuration options for the DownloadService.
    /// </summary>
    public class DownloadServiceConfig : IEquatable<DownloadServiceConfig>
    {
        /// <summary>
        /// Default for <see cref="DownloadRetryTimespan"/>
        /// </summary>
        private readonly TimeSpan DefaultDownloadRetryTimespan = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Default for <see cref="DownloadWaitTimeout"/>
        /// </summary>
        private static TimeSpan DefaultDownloadWaitTimeout = TimeSpan.FromHours(1);

        /// <summary>
        /// DownloadRetryTimespan in seconds.
        /// </summary>
        public double DownloadRetryTimespanInSeconds => DownloadRetryTimespan.TotalSeconds;

        /// <summary>
        /// The time we will wait between calling the API for a segmentation status.
        /// </summary>
        [JsonIgnore]
        public TimeSpan DownloadRetryTimespan { get; }

        /// <summary>
        /// DownloadWaitTimeout in seconds.
        /// </summary>
        public double DownloadWaitTimeoutInSeconds => DownloadWaitTimeout.TotalSeconds;

        /// <summary>
        /// The maximum time the service will wait for a result to finish.
        /// </summary>
        [JsonIgnore]
        public TimeSpan DownloadWaitTimeout { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="DownloadServiceConfig"/> class.
        /// </summary>
        /// <param name="downloadRetryTimespanInSeconds">Download retry timespan in seconds.</param>
        /// <param name="downloadWaitTimeoutInSeconds">Download wait timeout in seconds.</param>
        public DownloadServiceConfig(
            double? downloadRetryTimespanInSeconds = null,
            double? downloadWaitTimeoutInSeconds = null)
        {
            DownloadRetryTimespan = downloadRetryTimespanInSeconds.HasValue ?
                TimeSpan.FromSeconds(downloadRetryTimespanInSeconds.Value) : DefaultDownloadRetryTimespan;

            DownloadWaitTimeout = downloadWaitTimeoutInSeconds.HasValue ?
                TimeSpan.FromSeconds(downloadWaitTimeoutInSeconds.Value) : DefaultDownloadWaitTimeout;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as DownloadServiceConfig);
        }

        /// <inheritdoc/>
        public bool Equals(DownloadServiceConfig other)
        {
            return other != null &&
                   DefaultDownloadRetryTimespan.Equals(other.DefaultDownloadRetryTimespan) &&
                   DownloadRetryTimespan.Equals(other.DownloadRetryTimespan) &&
                   DownloadWaitTimeout.Equals(other.DownloadWaitTimeout);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -37651564;
            hashCode = hashCode * -1521134295 + DefaultDownloadRetryTimespan.GetHashCode();
            hashCode = hashCode * -1521134295 + DownloadRetryTimespan.GetHashCode();
            hashCode = hashCode * -1521134295 + DownloadWaitTimeout.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(DownloadServiceConfig left, DownloadServiceConfig right)
        {
            return EqualityComparer<DownloadServiceConfig>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(DownloadServiceConfig left, DownloadServiceConfig right)
        {
            return !(left == right);
        }
    }
}
