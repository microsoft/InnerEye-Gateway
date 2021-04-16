namespace Microsoft.InnerEye.Listener.Common.Services
{
    using System;

    /// <summary>
    /// Wrapped windows service interface.
    /// </summary>
    public interface IService : IDisposable
    {
        /// <summary>
        /// Called when the service wishes to stop.
        /// </summary>
        event EventHandler<EventArgs> StopRequested;

        /// <summary>
        /// Starts the service.
        /// </summary>
        void Start();

        /// <summary>
        /// Called when the service is stopping.
        /// </summary>
        void OnStop();
    }
}