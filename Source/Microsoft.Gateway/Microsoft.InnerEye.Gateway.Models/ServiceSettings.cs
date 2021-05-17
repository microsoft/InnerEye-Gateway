// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Service settings class
    /// </summary>
    public class ServiceSettings : IEquatable<ServiceSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceSettings"/> class.
        /// </summary>
        /// <param name="runAsConsole">If we should run the services as a console application.</param>
        public ServiceSettings(
            bool runAsConsole)
        {
            RunAsConsole = runAsConsole;
        }

        /// <summary>
        /// Gets if we should run the Windows Services as a console application.
        /// </summary>
        /// <value>
        /// If we should run the Windows Services as a console application.
        /// </value>
        public bool RunAsConsole { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ServiceSettings);
        }

        /// <inheritdoc/>
        public bool Equals(ServiceSettings other)
        {
            return other != null &&
                   RunAsConsole == other.RunAsConsole;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return 450039479 + RunAsConsole.GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(ServiceSettings left, ServiceSettings right)
        {
            return EqualityComparer<ServiceSettings>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ServiceSettings left, ServiceSettings right)
        {
            return !(left == right);
        }
    }
}
