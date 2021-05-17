// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Model containing all configuration for starting the Gateway.
    /// </summary>
    public class ConfigurationServiceConfig : IEquatable<ConfigurationServiceConfig>
    {
        /// <summary>
        /// Default configuration refresh delay, in seconds.
        /// </summary>
        public static readonly double DefaultConfigurationRefreshDelaySeconds = 60.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationServiceConfig"/> class.
        /// </summary>
        /// <param name="configCreationDateTime">The configuration creation date time.</param>
        /// <param name="applyConfigDateTime">The apply configuration date time.</param>
        /// <param name="configurationRefreshDelaySeconds">Time between refreshing configuration.</param>
        public ConfigurationServiceConfig(
            DateTime? configCreationDateTime = null,
            DateTime? applyConfigDateTime = null,
            double? configurationRefreshDelaySeconds = null)
        {
            ConfigCreationDateTime = configCreationDateTime ?? DateTime.UtcNow;
            ApplyConfigDateTime = applyConfigDateTime ?? DateTime.UtcNow;
            ConfigurationRefreshDelay = TimeSpan.FromSeconds(configurationRefreshDelaySeconds ?? DefaultConfigurationRefreshDelaySeconds);
        }

        /// <summary>
        /// Gets the configuration creation date time.
        /// </summary>
        /// <value>
        /// The configuration creation date time.
        /// </value>
        public DateTime ConfigCreationDateTime { get; }

        /// <summary>
        /// Gets the datetime when this config should be applied.
        /// </summary>
        /// <value>
        /// The datetime when this config should be applied
        /// </value>
        public DateTime ApplyConfigDateTime { get; }

        /// <summary>
        /// ConfigurationRefreshDelay in seconds.
        /// </summary>
        public double ConfigurationRefreshDelaySeconds => ConfigurationRefreshDelay.TotalSeconds;

        /// <summary>
        /// The time between refreshing the current service configuration.
        /// </summary>
        [JsonIgnore]
        public TimeSpan ConfigurationRefreshDelay { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigurationServiceConfig);
        }

        /// <inheritdoc/>
        public bool Equals(ConfigurationServiceConfig other)
        {
            return other != null &&
                   ConfigCreationDateTime == other.ConfigCreationDateTime &&
                   ApplyConfigDateTime == other.ApplyConfigDateTime &&
                   ConfigurationRefreshDelay.Equals(other.ConfigurationRefreshDelay);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -1437313058;
            hashCode = hashCode * -1521134295 + ConfigCreationDateTime.GetHashCode();
            hashCode = hashCode * -1521134295 + ApplyConfigDateTime.GetHashCode();
            hashCode = hashCode * -1521134295 + ConfigurationRefreshDelay.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(ConfigurationServiceConfig left, ConfigurationServiceConfig right)
        {
            return EqualityComparer<ConfigurationServiceConfig>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ConfigurationServiceConfig left, ConfigurationServiceConfig right)
        {
            return !(left == right);
        }
    }
}
