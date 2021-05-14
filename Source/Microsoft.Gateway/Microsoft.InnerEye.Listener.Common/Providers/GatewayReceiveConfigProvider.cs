// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Models;

    /// <summary>
    /// Monitor a JSON file containing a <see cref="GatewayReceiveConfig"/>.
    /// </summary>
    public class GatewayReceiveConfigProvider : BaseConfigProvider<GatewayReceiveConfig>
    {
        /// <summary>
        /// File name for JSON file containing a <see cref="GatewayReceiveConfig"/>.
        /// </summary>
        public static readonly string GatewayReceiveConfigFileName = "GatewayReceiveConfig.json";

        /// <summary>
        /// Initialize a new instance of the <see cref="GatewayReceiveConfigProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configurationsPathRoot">Path to folder containing GatewayReceiveConfigFileName.</param>
        public GatewayReceiveConfigProvider(
            ILogger logger,
            string configurationsPathRoot) : base(logger,
                configurationsPathRoot, GatewayReceiveConfigFileName)
        {
        }

        /// <summary>
        /// Set ServiceSettings.RunAsConsole.
        /// </summary>
        /// <param name="runAsConsole">If we should run the service as a console application.</param>
        public void SetRunAsConsole(bool runAsConsole) =>
            Update(gatewayReceiveConfig => gatewayReceiveConfig.With(new ServiceSettings(runAsConsole)));

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning <see cref="ServiceSettings"/> from cached <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <returns>Cached <see cref="ServiceSettings"/>.</returns>
        public ServiceSettings ServiceSettings() =>
            Config.ServiceSettings;

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning <see cref="ConfigurationServiceConfig"/> from cached <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <returns>Cached <see cref="ConfigurationServiceConfig"/>.</returns>
        public ConfigurationServiceConfig ConfigurationServiceConfig() =>
            Config.ConfigurationServiceConfig;

        /// <summary>
        /// Helper to create a <see cref="Func{TResult}"/> for returning <see cref="ReceiveServiceConfig"/> from cached <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <returns>Cached <see cref="ReceiveServiceConfig"/>.</returns>
        public ReceiveServiceConfig ReceiveServiceConfig() =>
            Config.ReceiveServiceConfig;
    }
}
