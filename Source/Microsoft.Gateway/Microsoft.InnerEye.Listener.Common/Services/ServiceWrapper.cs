namespace Microsoft.InnerEye.Listener.Common.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.ServiceProcess;

    /// <summary>
    /// Windows service wrapper so we can unit test window services.
    /// </summary>
    public class ServiceWrapper : ServiceBase
    {
        /// <summary>
        /// The services.
        /// </summary>
        private readonly List<IService> _services;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="service">The service to start.</param>
        public ServiceWrapper(string serviceName, params IService[] services)
        {
            _services = services.ToList();
            _services.ForEach(x => x.StopRequested += Service_StopRequested);

            ServiceName = serviceName;
        }

        /// <summary>
        /// Overrides the on start method and calls the service start method.
        /// </summary>
        /// <param name="args">The start arguments.</param>
        protected override void OnStart(string[] args)
        {
            Trace.TraceInformation(FormatLogStatement(nameof(OnStart)));

            base.OnStart(args);

            _services.ForEach(x => x.Start());
        }

        /// <summary>
        /// Overrides the on stop method and calls stop on the service.
        /// </summary>
        protected override void OnStop()
        {
            Trace.TraceInformation(FormatLogStatement(nameof(OnStop)));

            base.OnStop();

            _services.ForEach(x => x.OnStop());
        }

        /// <summary>
        /// Overrides the dispose method and calls dispose on the service.
        /// </summary>
        /// <param name="disposing">If we are dispsoing.</param>
        protected override void Dispose(bool disposing)
        {
            Trace.TraceInformation(FormatLogStatement(nameof(Dispose)));

            base.Dispose(disposing);

            if (disposing)
            {
                foreach (var service in _services)
                {
                    service.StopRequested -= Service_StopRequested;
                    service.Dispose();
                }
            }
        }

        /// <summary>
        /// Called when the service wishes to stop.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event args.</param>
        private void Service_StopRequested(object sender, EventArgs e)
        {
            if (CanStop)
            {
                Stop();
            }
        }

        /// <summary>
        /// Gets for formatted statement for logging.
        /// </summary>
        /// <param name="value">The inner statement.</param>
        /// <returns>The formatted log statement.</returns>
        private string FormatLogStatement(string value) =>
            string.Format(CultureInfo.InvariantCulture, "[{0}] {1}.", ServiceName, value);
    }
}