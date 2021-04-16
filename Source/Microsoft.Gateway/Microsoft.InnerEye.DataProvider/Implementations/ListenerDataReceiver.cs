namespace Microsoft.InnerEye.Listener.DataProvider.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using DataProvider.Interfaces;
    using DataProvider.Models;

    using global::Dicom;
    using global::Dicom.Network;

    /// <summary>
    /// Dicom data receiver implementation.
    /// </summary>
    public class ListenerDataReceiver : IDicomDataReceiver
    {
        /// <summary>
        /// The Dicom saver.
        /// </summary>
        private readonly IDicomSaver _dicomSaver;

        /// <summary>
        /// The Dicom server.
        /// </summary>
        private IDicomServer _dicomServer;

        /// <summary>
        /// If this instance is disposed.
        /// </summary>
        private bool _isDisposed = false;

        /// <summary>
        /// Constructor. This class is disposable.
        /// </summary>
        /// <param name="dicomImageSaver">The Dicom saver.</param>
        public ListenerDataReceiver(IDicomSaver dicomSaver)
        {
            _dicomSaver = dicomSaver;
        }

        /// <summary>
        /// Gets if the server is currently listening.
        /// </summary>
        public bool IsListening => _dicomServer?.IsListening ?? false;

        /// <summary>
        /// Data received event - this can be called from multiple different threads.
        /// </summary>
        public event EventHandler<DicomDataReceiverProgressEventArgs> DataReceived;

        /// <inheritdoc />
        /// <exception cref="DicomNetworkException">If the service is already listening.</exception>
        /// <exception cref="System.Net.Sockets.SocketException">If another service is already listening on this socket.</exception>
        public bool StartServer(int port, Func<IReadOnlyDictionary<DicomUID, DicomTransferSyntax[]>> getAcceptedTransferSyntaxes, TimeSpan timeout)
        {
            if (!ApplicationEntityValidationHelpers.ValidatePort(port))
            {
                throw new ArgumentException("The port is not valid.", nameof(port));
            }

            // Check if we are already listening
            if (IsListening)
            {
                throw new DicomNetworkException("We are already listening. Please call stop server before starting.");
            }

            // Looks like StartServer has been called before but failed to start listening.
            // Lets dispose of the current instance and try again.
            if (_dicomServer != null)
            {
                DisposeDicomServer();
            }

            var fileStoreParameters = new DicomFileStoreParameters(DicomDataReceiverUpdate, getAcceptedTransferSyntaxes, _dicomSaver);

            // Preload dictionary to prevent timeouts
            DicomDictionary.EnsureDefaultDictionariesLoaded();

            // Constructing the listener dicom server will attempt to start the service.
            _dicomServer = DicomServer.Create<ListenerDicomService>(ipAddress: "localhost", port: port, userState: fileStoreParameters);

            // Wait until listening or we have an exception.
            SpinWait.SpinUntil(() => _dicomServer.IsListening || _dicomServer.Exception != null, timeout);

            if (_dicomServer.Exception != null)
            {
                // Throw any exceptions
                throw _dicomServer.Exception;
            }

            return _dicomServer?.IsListening ?? false;
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            DisposeDicomServer();
        }

        /// <summary>
        /// Implements the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of this instance by calling stop server.
        /// </summary>
        /// <param name="disposing">If we are current disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                DisposeDicomServer();
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Disposes of the current dicom server instance.
        /// </summary>
        private void DisposeDicomServer()
        {
            if (_dicomServer != null)
            {
                _dicomServer.Stop();
                //_dicomServer.BackgroundWorker.Wait();

                _dicomServer.Dispose();

                _dicomServer = null;
            }
        }

        /// <summary>
        /// Data receiver update method.
        /// </summary>
        /// <param name="associationId">The identifier for this association.</param>
        /// <param name="sockectConnectionDateTime">The date time the socket connection started..</param>
        /// <param name="dicomAssociation">The Dicom association object.</param>
        /// <param name="progressCode">The progress code.</param>
        private void DicomDataReceiverUpdate(
            Guid associationId,
            DateTime socketConnectionDateTime,
            DicomAssociation dicomAssociation,
            DicomReceiveProgressCode progressCode)
        {
            DataReceived?.Invoke(
                this,
                new DicomDataReceiverProgressEventArgs(
                    dicomSaver: _dicomSaver,
                    progressCode: progressCode,
                    socketConnectionDateTime: socketConnectionDateTime,
                    dicomAssociation: dicomAssociation,
                    associationId: associationId));
        }
    }
}