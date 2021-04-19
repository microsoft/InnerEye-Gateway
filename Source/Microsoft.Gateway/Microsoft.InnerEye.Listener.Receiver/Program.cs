namespace Microsoft.InnerEye.Listener.Receiver
{
    using Common.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Services;

    public static class Program
    {
        /// <summary>
        /// The service name.
        /// </summary>
        public const string ServiceName = ServiceNames.ReceiveServiceName;

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

                var gatewayReceiveConfigProvider = new GatewayReceiveConfigProvider(
                    loggerFactory.CreateLogger("ProcessorSettings"),
                    configurationsPathRoot);

                // The ProjectInstaller.cs uses the service name to install the service.
                // If you change it please update the ProjectInstaller.cs
                ServiceHelpers.RunServices(
                    ServiceName,
                    gatewayReceiveConfigProvider.ServiceSettings(),
                    new ConfigurationService(
                        null,
                        gatewayReceiveConfigProvider.ConfigurationServiceConfig,
                        loggerFactory.CreateLogger("ConfigurationService"),
                        new ReceiveService(
                            gatewayReceiveConfigProvider.ReceiveServiceConfig,
                            GatewayMessageQueue.UploadQueuePath,
                            loggerFactory.CreateLogger("ReceiveService"))));
            }
        }
    }
}
