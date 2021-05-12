namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using System;
    using System.Globalization;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
    using Microsoft.InnerEye.Gateway.Logging;
    using Microsoft.InnerEye.Gateway.Models;

    /// <summary>
    /// Monitor a JSON file containing a <see cref="GatewayProcessorConfig"/>.
    /// </summary>
    public class GatewayProcessorConfigProvider : BaseConfigProvider<GatewayProcessorConfig>
    {
        /// <summary>
        /// File name for JSON file containing a <see cref="GatewayProcessorConfig"/>.
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
                configurationsPathRoot, GatewayProcessorConfigFileName)
        {
        }

        /// <summary>
        /// Set ServiceSettings.RunAsConsole.
        /// </summary>
        /// <param name="runAsConsole">If we should run the service as a console application.</param>
        public void SetRunAsConsole(bool runAsConsole) =>
            Update(gatewayProcessorConfig => gatewayProcessorConfig.With(new ServiceSettings(runAsConsole)));

        /// <summary>
        /// Update <see cref="ProcessorSettings"/>.
        /// </summary>
        /// <param name="inferenceUri">Optional new inference API Uri.</param>
        /// <param name="licenseKey">Optional new license key.</param>
        public void SetProcessorSettings(Uri inferenceUri = null, string licenseKey = null)
        {
            if (inferenceUri != null)
            {
                Update(gatewayProcessorConfig =>
                    gatewayProcessorConfig.With(
                        processorSettings: gatewayProcessorConfig.ProcessorSettings.With(
                            inferenceUri: inferenceUri)));
            }

            if (licenseKey != null)
            {
                var processorSettings = ProcessorSettings();

                Environment.SetEnvironmentVariable(processorSettings.LicenseKeyEnvVar, licenseKey, EnvironmentVariableTarget.Machine);
            }
        }

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning <see cref="ServiceSettings"/> from cached <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <returns>Cached <see cref="ServiceSettings"/>.</returns>
        public ServiceSettings ServiceSettings() =>
            Config.ServiceSettings;

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning <see cref="ProcessorSettings"/> from cached <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <returns>Cached <see cref="ProcessorSettings"/>.</returns>
        public ProcessorSettings ProcessorSettings() =>
            Config.ProcessorSettings;

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning <see cref="DequeueServiceConfig"/> from cached <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <returns>Cached <see cref="DequeueServiceConfig"/>.</returns>
        public DequeueServiceConfig DequeueServiceConfig() =>
            Config.DequeueServiceConfig;

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning <see cref="DownloadServiceConfig"/> from cached <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <returns>Cached <see cref="DownloadServiceConfig"/>.</returns>
        public DownloadServiceConfig DownloadServiceConfig() =>
            Config.DownloadServiceConfig;

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning <see cref="ConfigurationServiceConfig"/> from cached <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <returns>Cached <see cref="ConfigurationServiceConfig"/>.</returns>
        public ConfigurationServiceConfig ConfigurationServiceConfig() =>
            Config.ConfigurationServiceConfig;

        /// <summary>
        /// Create a new <see cref="IInnerEyeSegmentationClient"/> based on settings in JSON file.
        /// </summary>
        /// <param name="logger">Optional logger for client.</param>
        /// <returns>New <see cref="IInnerEyeSegmentationClient"/>.</returns>
        public Func<IInnerEyeSegmentationClient> CreateInnerEyeSegmentationClient(ILogger logger = null) =>
            () =>
            {
                var processorSettings = ProcessorSettings();

                var licenseKey = processorSettings.LicenseKey;

                if (string.IsNullOrEmpty(licenseKey))
                {
                    var message = string.Format(CultureInfo.InvariantCulture, "License key for the service `{0}` has not been set correctly in environment variable `{1}`. It needs to be a system variable.",
                        processorSettings.InferenceUri, processorSettings.LicenseKeyEnvVar);
                    var logEntry = LogEntry.Create(ServiceStatus.Starting);
                    logEntry.Log(logger, LogLevel.Error, new ConfigurationException(message));
                }

                return new InnerEyeSegmentationClient(processorSettings.InferenceUri, licenseKey);
            };
    }
}
