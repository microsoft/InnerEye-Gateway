// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Logging
{
    /// <summary>
    /// The service status logging enumeration.
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>
        /// Service starting up.
        /// </summary>
        Starting,

        /// <summary>
        /// Service started.
        /// </summary>
        Started,

        /// <summary>
        /// Error starting service.
        /// </summary>
        StartError,

        /// <summary>
        /// Service stopping.
        /// </summary>
        Stopping,

        /// <summary>
        /// Service stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// Error stopping service.
        /// </summary>
        StoppingError,

        /// <summary>
        /// Service is updating.
        /// </summary>
        Updating,

        /// <summary>
        /// A new configuration for the service has been detected and is
        /// ready to be applied.
        /// </summary>
        NewConfigurationAvailable,

        /// <summary>
        /// A new configuration has been applied to the services.
        /// </summary>
        NewConfigurationApplied,

        /// <summary>
        /// Error loading new configuration.
        /// </summary>
        NewConfigurationError,

        /// <summary>
        /// Configuration files have changed.
        /// </summary>
        NewConfigurationDetetected,

        /// <summary>
        /// Error in ping.
        /// </summary>
        PingError,

        /// <summary>
        /// Error in execute.
        /// </summary>
        ExecuteError,

        /// <summary>
        /// Error in GetAcceptedSopClassesAndTransferSyntaxes.
        /// </summary>
        GetAcceptedSopClassesAndTransferSyntaxesError,
    }
}