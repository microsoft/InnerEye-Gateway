namespace Microsoft.InnerEye.Listener.Processor
{
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.InnerEye.Listener.Common.Services;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.Processor.Services;

    public static class Program
    {
        /// <summary>
        /// The service name.
        /// </summary>
        public const string ServiceName = ServiceNames.ProcessorServiceName;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main()
        {
            // Create the loggerFactory as Console + Log4Net.
            using (var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddLog4Net();
            }))
            {
                var relativePaths = new[] {
                    "../Config",
                    "../../../../../SampleConfigurations"
                };

                var configurationsPathRoot = ConfigurationService.FindRelativeDirectory(relativePaths, loggerFactory.CreateLogger("Main"));

                using (var aetConfigurationProvider = new AETConfigProvider(
                        loggerFactory.CreateLogger("ModelSettings"),
                        configurationsPathRoot))
                using (var gatewayProcessorConfigProvider = new GatewayProcessorConfigProvider(
                        loggerFactory.CreateLogger("ProcessorSettings"),
                        configurationsPathRoot))
                {
                    var segmentationClientLogger = loggerFactory.CreateLogger("SegmentationClient");

                    // The ProjectInstaller.cs uses the service name to install the service.
                    // If you change it please update the ProjectInstaller.cs
                    ServiceHelpers.RunServices(
                    ServiceName,
                    gatewayProcessorConfigProvider.ServiceSettings(),
                    new ConfigurationService(
                        gatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient(segmentationClientLogger),
                        gatewayProcessorConfigProvider.ConfigurationServiceConfig,
                        loggerFactory.CreateLogger("ConfigurationService"),
                        new UploadService(
                            gatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient(segmentationClientLogger),
                            aetConfigurationProvider.AETConfigModels,
                            GatewayMessageQueue.UploadQueuePath,
                            GatewayMessageQueue.DownloadQueuePath,
                            GatewayMessageQueue.DeleteQueuePath,
                            gatewayProcessorConfigProvider.DequeueServiceConfig,
                            loggerFactory.CreateLogger("UploadService"),
                            instances: 2),
                        new DownloadService(
                            gatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient(segmentationClientLogger),
                            GatewayMessageQueue.DownloadQueuePath,
                            GatewayMessageQueue.PushQueuePath,
                            GatewayMessageQueue.DeleteQueuePath,
                            gatewayProcessorConfigProvider.DownloadServiceConfig,
                            gatewayProcessorConfigProvider.DequeueServiceConfig,
                            loggerFactory.CreateLogger("DownloadService"),
                            instances: 1),
                        new PushService(
                            aetConfigurationProvider.AETConfigModels,
                            new DicomDataSender(),
                            GatewayMessageQueue.PushQueuePath,
                            GatewayMessageQueue.DeleteQueuePath,
                            gatewayProcessorConfigProvider.DequeueServiceConfig,
                            loggerFactory.CreateLogger("PushService"),
                            instances: 1),
                        new DeleteService(
                            GatewayMessageQueue.DeleteQueuePath,
                            gatewayProcessorConfigProvider.DequeueServiceConfig,
                            loggerFactory.CreateLogger("DeleteService"))));
                }
            }
        }
    }
}
