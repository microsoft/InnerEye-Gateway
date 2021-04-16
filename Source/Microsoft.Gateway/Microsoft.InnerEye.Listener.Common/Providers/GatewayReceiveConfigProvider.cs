namespace Microsoft.InnerEye.Listener.Common.Providers
{
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
        /// Initialize a new instance of the <see cref="GatewayReceiveConfigProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configurationsPathRoot">Path to folder containing GatewayReceiveConfigFileName.</param>
        public GatewayReceiveConfigProvider(
            ILogger logger,
            string configurationsPathRoot) : base(logger,
                Path.Combine(configurationsPathRoot, GatewayReceiveConfigFileName))
        {
        }

        /// <summary>
        /// Load GatewayReceiveConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded GatewayReceiveConfig.</returns>
        public GatewayReceiveConfig GatewayReceiveConfig()
        {
            Load();
            return _t;
        }

        /// <summary>
        /// Load ServiceSettings from a JSON file.
        /// </summary>
        /// <returns>Loaded ServiceSettings.</returns>
        public ServiceSettings ServiceSettings() =>
            GatewayReceiveConfig().ServiceSettings;

        /// <summary>
        /// Load ConfigurationServiceConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded ConfigurationServiceConfig.</returns>
        public ConfigurationServiceConfig ConfigurationServiceConfig() =>
            GatewayReceiveConfig().ConfigurationServiceConfig;

        /// <summary>
        /// Load ReceiveServiceConfig from a JSON file.
        /// </summary>
        /// <returns>Loaded ReceiveServiceConfig.</returns>
        public ReceiveServiceConfig ReceiveServiceConfig() =>
            GatewayReceiveConfig().ReceiveServiceConfig;
    }
}
