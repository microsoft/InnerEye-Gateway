namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
    using Microsoft.InnerEye.Gateway.Models;

    /// <summary>
    /// Monitor a JSON file containing a GatewayProcessorConfig.
    /// </summary>
    public class GatewayProcessorConfigProvider : BaseConfigProvider<GatewayProcessorConfig>
    {
        /// <summary>
        /// File name for JSON file containing a GatewayProcessorConfig.
        /// </summary>
        public static readonly string GatewayProcessorConfigFileName = "GatewayProcessorConfig.json";

        /// <summary>
        /// Initialize a new instance of the <see cref="GatewayProcessorConfigProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configurationsPathRoot">Path to folder containing GatewayProcessorConfigFileName.</param>
        public GatewayProcessorConfigProvider(
            ILogger logger,
            string configurationsPathRoot) : base(logger,
                Path.Combine(configurationsPathRoot, GatewayProcessorConfigFileName))
        {
        }

        /// <summary>
        /// Load GatewayProcessorConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded GatewayProcessorConfig.</returns>
        public GatewayProcessorConfig GatewayProcessorConfig()
        {
            Load();
            return _t;
        }

        /// <summary>
        /// Update GatewayProcessorConfig file, according to an update callback function.
        /// </summary>
        /// <param name="updater">Callback to update the settings. Return new settings for update, or the same object to not update.</param>
        public void Update(Func<GatewayProcessorConfig, GatewayProcessorConfig> updater) =>
            UpdateFile(updater, EqualityComparer<GatewayProcessorConfig>.Default);

        /// <summary>
        /// Set ServiceSettings.RunAsConsole.
        /// </summary>
        /// <param name="runAsConsole">If we should run the services as a console application.</param>
        public void SetRunAsConsole(bool runAsConsole) =>
            Update(gatewayProcessorConfig => gatewayProcessorConfig.With(new ServiceSettings(runAsConsole)));

        /// <summary>
        /// Set ProcessorSettings.InferenceUri.
        /// </summary>
        /// <param name="inferenceUri">Inference API Uri.</param>
        public void SetInferenceUri(Uri inferenceUri) =>
            Update(gatewayProcessorConfig =>
                gatewayProcessorConfig.With(
                    processorSettings: gatewayProcessorConfig.ProcessorSettings.With(
                        inferenceUri: inferenceUri)));

        /// <summary>
        /// Load ServiceSettings from a JSON file.
        /// </summary>
        /// <returns>Loaded ServiceSettings.</returns>
        public ServiceSettings ServiceSettings() =>
            GatewayProcessorConfig().ServiceSettings;

        /// <summary>
        /// Load ProcessorSettings from a JSON file.
        /// </summary>
        /// <returns>Loaded ProcessorSettings.</returns>
        public ProcessorSettings ProcessorSettings() =>
            GatewayProcessorConfig().ProcessorSettings;

        /// <summary>
        /// Load DequeueServiceConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded DequeueServiceConfig.</returns>
        public DequeueServiceConfig DequeueServiceConfig() =>
            GatewayProcessorConfig().DequeueServiceConfig;

        /// <summary>
        /// Load DownloadServiceConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded DownloadServiceConfig.</returns>
        public DownloadServiceConfig DownloadServiceConfig() =>
            GatewayProcessorConfig().DownloadServiceConfig;

        /// <summary>
        /// Load ConfigurationServiceConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded ConfigurationServiceConfig.</returns>
        public ConfigurationServiceConfig ConfigurationServiceConfig() =>
            GatewayProcessorConfig().ConfigurationServiceConfig;

        /// <summary>
        /// Create a new segmentation client based on settings in JSON file.
        /// </summary>
        /// <param name="logger">Optional logger for client.</param>
        /// <param name="licenseKeyEnvVar">Optional override license key env var for testing.</param>
        /// <returns>New IInnerEyeSegmentationClient.</returns>
        public Func<IInnerEyeSegmentationClient> CreateInnerEyeSegmentationClient(ILogger logger = null, string licenseKeyEnvVar = null) =>
            () =>
            {
                var settings = ProcessorSettings();

                return new InnerEyeSegmentationClient(settings.InferenceUri, licenseKeyEnvVar ?? settings.LicenseKeyEnvVar, logger);
            };
    }
}
