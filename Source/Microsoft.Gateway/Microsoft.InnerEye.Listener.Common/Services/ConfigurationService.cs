
namespace Microsoft.InnerEye.Listener.Common.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
    using Microsoft.InnerEye.Gateway.Logging;
    using Microsoft.InnerEye.Gateway.Models;

    /// <summary>
    /// The receive configuration service, used for starting and stopping the receive service base on configuration changes,
    /// </summary>
    /// <seealso cref="ThreadedServiceBase" />
    public sealed class ConfigurationService : ThreadedServiceBase
    {
        /// <summary>
        /// Callback to create InnerEye segmentation client.
        /// </summary>
        private readonly Func<IInnerEyeSegmentationClient> _getInnerEyeSegmentationClient;

        /// <summary>
        /// The InnerEye segmentation client.
        /// </summary>
        private IInnerEyeSegmentationClient _innerEyeSegmentationClient;

        /// <summary>
        /// The services this instance manages.
        /// </summary>
        private readonly List<IService> _services;

        /// <summary>
        /// The get gateway configuration function.
        /// </summary>
        private readonly Func<ConfigurationServiceConfig> _getConfigurationServiceConfig;

        /// <summary>
        /// The current configuration service configuration.
        /// </summary>
        private ConfigurationServiceConfig _configurationServiceConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
        /// </summary>
        /// <param name="getInnerEyeSegmentationClient">Callback to create InnerEye segmentation client.</param>
        /// <param name="getConfigurationServiceConfig">Configuration service config callback.</param>
        /// <param name="logger">The log.</param>
        /// <param name="services">The services.</param>
        /// <exception cref="ArgumentNullException">getGatewayConfigurationFunction, services or getGatewayConfigurationFunction</exception>
        public ConfigurationService(
            Func<IInnerEyeSegmentationClient> getInnerEyeSegmentationClient,
            Func<ConfigurationServiceConfig> getConfigurationServiceConfig,
            ILogger logger,
            params IService[] services)
            : base(logger, 1)
        {
            _getInnerEyeSegmentationClient = getInnerEyeSegmentationClient;
            _getConfigurationServiceConfig = getConfigurationServiceConfig ?? throw new ArgumentNullException(nameof(getConfigurationServiceConfig));

            _services = services.ToList() ?? throw new ArgumentNullException(nameof(services));
            _services.ForEach(x => x.StopRequested += Service_StopRequested);
        }

        /// <summary>
        /// Called when the service is started.
        /// </summary>
        protected override void OnServiceStart()
        {
            _innerEyeSegmentationClient?.Dispose();
            _innerEyeSegmentationClient = null;

            if (_getInnerEyeSegmentationClient != null)
            {
                _innerEyeSegmentationClient = _getInnerEyeSegmentationClient.Invoke();

                Task.WaitAll(PingAsync(stopServiceOnAuthFailures: false));
            }

            _configurationServiceConfig = _getConfigurationServiceConfig();

            // Initialize the log
            LogInformation(LogEntry.CreateInitialize());

            // Start the services.
            _services.ForEach(x => x.Start());
        }

        /// <summary>
        /// Called when [service stop].
        /// </summary>
        protected override void OnServiceStop()
        {
            _services.ForEach(x => x.OnStop());
        }

        /// <summary>
        /// Called when [update tick] is called. This will wait for all work to execute then will pause for desired interval delay.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The async task.
        /// </returns>
        protected override async Task OnUpdateTickAsync(CancellationToken cancellationToken)
        {
            // Check we can still ping with the license key
            // This call will stop this service if the license key is invalid.
            await PingAsync();

            await Task.Delay(_configurationServiceConfig.ConfigurationRefreshDelay, cancellationToken);

            var config = _getConfigurationServiceConfig();

            if (config.ConfigCreationDateTime > _configurationServiceConfig.ConfigCreationDateTime
                && DateTime.UtcNow >= config.ApplyConfigDateTime)
            {
                LogInformation(LogEntry.Create(ServiceStatus.NewConfigurationAvailable));

                _innerEyeSegmentationClient?.Dispose();
                _innerEyeSegmentationClient = null;

                if (_getInnerEyeSegmentationClient != null)
                {
                    _innerEyeSegmentationClient = _getInnerEyeSegmentationClient.Invoke();
                }

                // Update the current configuration.
                _configurationServiceConfig = config;

                // Stop the services
                _services.ForEach(x => x.OnStop());

                // Re-initialize the log
                LogInformation(LogEntry.CreateInitialize());

                // Start the services again
                _services.ForEach(x => x.Start());

                LogInformation(LogEntry.Create(ServiceStatus.NewConfigurationApplied));
            }
        }

        /// <summary>
        /// Disposes of all managed resources.
        /// </summary>
        /// <param name="disposing">If we are disposing.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            _services.ForEach(x => x.StopRequested -= Service_StopRequested);
            _services.ForEach(x => x.Dispose());

            _innerEyeSegmentationClient?.Dispose();
            _innerEyeSegmentationClient = null;
        }

        /// <summary>
        /// Pings the segmentation client and stops the service on authentication exceptions.
        /// </summary>
        /// <param name="stopServiceOnAuthFailures">Will stop the service on any authentication failures.</param>
        /// <exception cref="AuthenticationException">If the license key is incorrect for the segmentation client.</exception>
        private async Task PingAsync(bool stopServiceOnAuthFailures = true)
        {
            try
            {
                if (_innerEyeSegmentationClient != null)
                {
                    await _innerEyeSegmentationClient.PingAsync();
                }
            }
            catch (AuthenticationException e)
            {
                LogError(LogEntry.Create(ServiceStatus.PingError), e);

                if (stopServiceOnAuthFailures)
                {
                    StopServiceAsync();
                }

                throw;
            }
        }

        /// <summary>
        /// Handles the StopRequested event of the Service control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Service_StopRequested(object sender, EventArgs e)
        {
            StopServiceAsync();
        }
    }
}
