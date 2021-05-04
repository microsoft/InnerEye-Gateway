﻿namespace Microsoft.InnerEye.Listener.Common.Providers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Models;

    /// <summary>
    /// Monitor a JSON file containing a GatewayReceiveConfig.
    /// </summary>
    public class GatewayReceiveConfigProvider : BaseConfigProvider<GatewayReceiveConfig>
    {
        /// <summary>
        /// File name for JSON file containing a GatewayReceiveConfig.
        /// </summary>
        public static readonly string GatewayReceiveConfigFileName = "GatewayReceiveConfig.json";

        /// <summary>
        /// GatewayReceiveConfig loaded from a JSON file.
        /// </summary>
        public GatewayReceiveConfig GatewayReceiveConfig { get; private set; }

        /// <summary>
        /// Initialize a new instance of the <see cref="GatewayReceiveConfigProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configurationsPathRoot">Path to folder containing GatewayReceiveConfigFileName.</param>
        public GatewayReceiveConfigProvider(
            ILogger logger,
            string configurationsPathRoot) : base(logger,
                Path.Combine(configurationsPathRoot, GatewayReceiveConfigFileName))
        {
            Reload();
        }

        public void Reload()
        {
            var (t, loaded, _) = Load();

            if (loaded)
            {
                GatewayReceiveConfig = t;
            }
        }

        /// <summary>
        /// Update GatewayReceiveConfig file, according to an update callback function.
        /// </summary>
        /// <param name="updater">Callback to update the settings. Return new settings for update, or the same object to not update.</param>
        public void Update(Func<GatewayReceiveConfig, GatewayReceiveConfig> updater)
        {
            var (newt, updated) = UpdateFile(updater, EqualityComparer<GatewayReceiveConfig>.Default);

            if (updated)
            {
                GatewayReceiveConfig = newt;
            }
        }

        /// <summary>
        /// Set ServiceSettings.RunAsConsole.
        /// </summary>
        /// <param name="runAsConsole">If we should run the service as a console application.</param>
        public void SetRunAsConsole(bool runAsConsole) =>
            Update(gatewayReceiveConfig => gatewayReceiveConfig.With(new ServiceSettings(runAsConsole)));

        /// <summary>
        /// Load ServiceSettings from a JSON file.
        /// </summary>
        /// <returns>Loaded ServiceSettings.</returns>
        public ServiceSettings ServiceSettings() =>
            GatewayReceiveConfig.ServiceSettings;

        /// <summary>
        /// Load ConfigurationServiceConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded ConfigurationServiceConfig.</returns>
        public ConfigurationServiceConfig ConfigurationServiceConfig()
        {
            Reload();

            return GatewayReceiveConfig.ConfigurationServiceConfig;
        }

        /// <summary>
        /// Load ReceiveServiceConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded ReceiveServiceConfig.</returns>
        public ReceiveServiceConfig ReceiveServiceConfig() =>
            GatewayReceiveConfig.ReceiveServiceConfig;
    }
}
