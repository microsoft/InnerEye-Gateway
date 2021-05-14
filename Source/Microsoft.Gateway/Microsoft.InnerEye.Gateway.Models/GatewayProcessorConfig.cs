// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration class for the Processor service.
    /// </summary>
    public class GatewayProcessorConfig : IEquatable<GatewayProcessorConfig>
    {
        /// <summary>
        /// Configuration for the Processor service itself.
        /// </summary>
        public ServiceSettings ServiceSettings { get; }

        /// <summary>
        /// Configuration for the inference API.
        /// </summary>
        public ProcessorSettings ProcessorSettings { get; }

        /// <summary>
        /// Configuration for the services, all are based on the <see cref="DequeueClientServiceBase"/> class.
        /// </summary>
        public DequeueServiceConfig DequeueServiceConfig { get; }

        /// <summary>
        /// Configuration for the download service.
        /// </summary>
        public DownloadServiceConfig DownloadServiceConfig { get; }

        /// <summary>
        /// Configuration for the configuration service.
        /// </summary>
        public ConfigurationServiceConfig ConfigurationServiceConfig { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GatewayProcessorConfig"/> class.
        /// </summary>
        /// <param name="serviceSettings">Service settings.</param>
        /// <param name="processorSettings">Processor settings.</param>
        /// <param name="dequeueServiceConfig">Dequeue service config.</param>
        /// <param name="downloadServiceConfig">Download service config.</param>
        /// <param name="configurationServiceConfig">Configuration service config.</param>
        public GatewayProcessorConfig(
            ServiceSettings serviceSettings,
            ProcessorSettings processorSettings,
            DequeueServiceConfig dequeueServiceConfig,
            DownloadServiceConfig downloadServiceConfig,
            ConfigurationServiceConfig configurationServiceConfig)
        {
            ServiceSettings = serviceSettings;
            ProcessorSettings = processorSettings;
            DequeueServiceConfig = dequeueServiceConfig;
            DownloadServiceConfig = downloadServiceConfig;
            ConfigurationServiceConfig = configurationServiceConfig;
        }

        /// <summary>
        /// Clone this into a new instance of the <see cref="GatewayProcessorConfig"/> class, optionally replacing some properties.
        /// </summary>
        /// <param name="serviceSettings">Optional new service settings.</param>
        /// <param name="processorSettings">Optional new processor settings.</param>
        /// <param name="dequeueServiceConfig">Optional new dequeue service config.</param>
        /// <param name="downloadServiceConfig">Optional new download service config.</param>
        /// <param name="configurationServiceConfig">Optional new configuration service config.</param>
        /// <returns>New GatewayProcessorConfig.</returns>
        public GatewayProcessorConfig With(
            ServiceSettings serviceSettings = null,
            ProcessorSettings processorSettings = null,
            DequeueServiceConfig dequeueServiceConfig = null,
            DownloadServiceConfig downloadServiceConfig = null,
            ConfigurationServiceConfig configurationServiceConfig = null) =>
                new GatewayProcessorConfig(
                    serviceSettings ?? ServiceSettings,
                    processorSettings ?? ProcessorSettings,
                    dequeueServiceConfig ?? DequeueServiceConfig,
                    downloadServiceConfig ?? DownloadServiceConfig,
                    configurationServiceConfig ?? ConfigurationServiceConfig);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as GatewayProcessorConfig);
        }

        /// <inheritdoc/>
        public bool Equals(GatewayProcessorConfig other)
        {
            return other != null &&
                   EqualityComparer<ServiceSettings>.Default.Equals(ServiceSettings, other.ServiceSettings) &&
                   EqualityComparer<ProcessorSettings>.Default.Equals(ProcessorSettings, other.ProcessorSettings) &&
                   EqualityComparer<DequeueServiceConfig>.Default.Equals(DequeueServiceConfig, other.DequeueServiceConfig) &&
                   EqualityComparer<DownloadServiceConfig>.Default.Equals(DownloadServiceConfig, other.DownloadServiceConfig) &&
                   EqualityComparer<ConfigurationServiceConfig>.Default.Equals(ConfigurationServiceConfig, other.ConfigurationServiceConfig);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -1353736591;
            hashCode = hashCode * -1521134295 + EqualityComparer<ServiceSettings>.Default.GetHashCode(ServiceSettings);
            hashCode = hashCode * -1521134295 + EqualityComparer<ProcessorSettings>.Default.GetHashCode(ProcessorSettings);
            hashCode = hashCode * -1521134295 + EqualityComparer<DequeueServiceConfig>.Default.GetHashCode(DequeueServiceConfig);
            hashCode = hashCode * -1521134295 + EqualityComparer<DownloadServiceConfig>.Default.GetHashCode(DownloadServiceConfig);
            hashCode = hashCode * -1521134295 + EqualityComparer<ConfigurationServiceConfig>.Default.GetHashCode(ConfigurationServiceConfig);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(GatewayProcessorConfig left, GatewayProcessorConfig right)
        {
            return EqualityComparer<GatewayProcessorConfig>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(GatewayProcessorConfig left, GatewayProcessorConfig right)
        {
            return !(left == right);
        }
    }
}
