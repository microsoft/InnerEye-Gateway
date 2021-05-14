// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// Gateway representation of an application entity.
    /// </summary>
    [Serializable]
    public class GatewayApplicationEntity
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="title">The application entity title.</param>
        /// <param name="port">The application entity port number.</param>
        /// <param name="ipAddress">The application entity IP address.</param>
        [JsonConstructor]
        public GatewayApplicationEntity(string title, int port, string ipAddress)
        {
            Title = title;
            Port = port;
            IpAddress = ipAddress;
        }

        /// <summary>
        /// Gets the application entity service title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the port number for the application entity.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the IP address of the application entity.
        /// </summary>
        public string IpAddress { get; }
    }
}