namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration class for the Receiver service.
    /// </summary>
    public class GatewayReceiveConfig : IEquatable<GatewayReceiveConfig>
    {
        /// <summary>
        /// Configuration for the Receiver service itself.
        /// </summary>
        public ServiceSettings ServiceSettings { get; }

        /// <summary>
        /// Configuration for the Receive subservice.
        /// </summary>
        public ReceiveServiceConfig ReceiveServiceConfig { get; }

        /// <summary>
        /// Configuration for the configuration service.
        /// </summary>
        public ConfigurationServiceConfig ConfigurationServiceConfig { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="GatewayReceiveConfig"/> class.
        /// </summary>
        /// <param name="serviceSettings">Service settings.</param>
        /// <param name="receiveServiceConfig">Receive service config.</param>
        /// <param name="configurationServiceConfig">Configuration service config.</param>
        public GatewayReceiveConfig(
            ServiceSettings serviceSettings,
            ReceiveServiceConfig receiveServiceConfig,
            ConfigurationServiceConfig configurationServiceConfig)
        {
            ServiceSettings = serviceSettings;
            ReceiveServiceConfig = receiveServiceConfig;
            ConfigurationServiceConfig = configurationServiceConfig;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as GatewayReceiveConfig);
        }

        /// <inheritdoc/>
        public bool Equals(GatewayReceiveConfig other)
        {
            return other != null &&
                   EqualityComparer<ServiceSettings>.Default.Equals(ServiceSettings, other.ServiceSettings) &&
                   EqualityComparer<ReceiveServiceConfig>.Default.Equals(ReceiveServiceConfig, other.ReceiveServiceConfig) &&
                   EqualityComparer<ConfigurationServiceConfig>.Default.Equals(ConfigurationServiceConfig, other.ConfigurationServiceConfig);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -1539593518;
            hashCode = hashCode * -1521134295 + EqualityComparer<ServiceSettings>.Default.GetHashCode(ServiceSettings);
            hashCode = hashCode * -1521134295 + EqualityComparer<ReceiveServiceConfig>.Default.GetHashCode(ReceiveServiceConfig);
            hashCode = hashCode * -1521134295 + EqualityComparer<ConfigurationServiceConfig>.Default.GetHashCode(ConfigurationServiceConfig);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(GatewayReceiveConfig left, GatewayReceiveConfig right)
        {
            return EqualityComparer<GatewayReceiveConfig>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(GatewayReceiveConfig left, GatewayReceiveConfig right)
        {
            return !(left == right);
        }
    }
}
