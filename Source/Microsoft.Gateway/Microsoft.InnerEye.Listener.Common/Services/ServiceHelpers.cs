// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Common.Services
{
    using System;
    using System.Net;
    using System.ServiceProcess;
    using System.Threading;
    using Microsoft.InnerEye.Gateway.Models;

    /// <summary>
    /// Helpers for window services.
    /// </summary>
    public static class ServiceHelpers
    {
        /// <summary>
        /// Helper for running services. As it is not possible to debug windows services
        /// in debug mode we start a thread manually for each service.
        /// </summary>
        /// <param name="serviceName">The service name.</param>
        /// <param name="serviceSettings">Service settings.</param>
        /// <param name="services">The services to start.</param>
        /// <exception cref="ArgumentException"></exception>
        public static void RunServices(string serviceName, ServiceSettings serviceSettings, params IService[] services)
        {
            serviceSettings = serviceSettings ?? throw new ArgumentNullException(nameof(serviceSettings));

            if (services.Length == 0)
            {
                throw new ArgumentException("Must provided at least one service to run.", nameof(services));
            }

            if (serviceSettings.RunAsConsole)
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                foreach (var service in services)
                {
                    new Thread(() => { service.Start(); }).Start();
                }

                Thread.Sleep(TimeSpan.FromDays(1));
            }
            else
            {
                using (var serviceWrapper = new ServiceWrapper(serviceName, services))
                {
                    ServiceBase.Run(serviceWrapper);
                }
            }
        }
    }
}