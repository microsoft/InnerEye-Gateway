// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Receiver.Services
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dicom.Network;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Logging;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common.Services;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Interfaces;
    using Microsoft.InnerEye.Listener.DataProvider.Models;

    /// <summary>
    /// The receive service.
    /// </summary>
    public sealed class ReceiveService : ThreadedServiceBase
    {
        /// <summary>
        /// The upload queue path.
        /// </summary>
        private readonly string _uploadQueuePath;

        /// <summary>
        /// Receiver configuration callback.
        /// </summary>
        private readonly Func<ReceiveServiceConfig> _getReceiveServiceConfig;

        /// <summary>
        /// The data receiver for receiving data over Dicom.
        /// </summary>
        private IDicomDataReceiver _dataReceiver;

        /// <summary>
        /// A cached reference of the gateway config.
        /// </summary>
        private ReceiveServiceConfig _receiveServiceConfig;

        /// <summary>
        /// Create a new IMessageQueue for the upload queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue UploadQueue => GatewayMessageQueue.Get(_uploadQueuePath);

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveService"/> class.
        /// </summary>
        /// <param name="getReceiveServiceConfig">Callback to get configuration.</param>
        /// <param name="uploadQueuePath">The upload queue path.</param>
        /// <param name="logger">The log.</param>
        public ReceiveService(
            Func<ReceiveServiceConfig> getReceiveServiceConfig,
            string uploadQueuePath,
            ILogger logger)
            : base(logger, 1)
        {
            _uploadQueuePath = !string.IsNullOrWhiteSpace(uploadQueuePath) ? uploadQueuePath : throw new ArgumentException("The upload queue path is null or white space.", nameof(uploadQueuePath));
            _getReceiveServiceConfig = getReceiveServiceConfig ?? throw new ArgumentNullException(nameof(getReceiveServiceConfig));
        }

        /// <summary>
        /// Called when the service is started.
        /// </summary>
        protected override void OnServiceStart()
        {
            LogTrace(LogEntry.Create(AssociationStatus.ReceiveServiceStart));

            _receiveServiceConfig = _getReceiveServiceConfig();

            // Create a folder for saving data and the data saver object.
            var imageSaver = new ListenerDicomSaver(_receiveServiceConfig.RootDicomFolder);

            _dataReceiver = new ListenerDataReceiver(imageSaver);
            _dataReceiver.DataReceived += DataReceiver_DataReceived;

            // Start listening
            var serverStarted = _dataReceiver.StartServer(
                    port: _receiveServiceConfig.GatewayDicomEndPoint.Port,
                    getAcceptedTransferSyntaxes: () => _receiveServiceConfig.AcceptedSopClassesAndTransferSyntaxes,
                    timeout: TimeSpan.FromSeconds(2));

            if (!serverStarted)
            {
                throw new ArgumentException("Failed to start the Dicom data receiver. The input configuration is not correct.", nameof(_receiveServiceConfig));
            }
        }

        /// <summary>
        /// Called when [service stop].
        /// </summary>
        protected override void OnServiceStop()
        {
            if (_dataReceiver == null)
            {
                return;
            }

            _dataReceiver.DataReceived -= DataReceiver_DataReceived;
            _dataReceiver.StopServer();
            _dataReceiver.Dispose();
            _dataReceiver = null;
        }

        /// <summary>
        /// Called when [update tick] is called. This will wait for all work to execute then will pause for desired interval delay.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The async task.
        /// </returns>
        protected override Task OnUpdateTickAsync(CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.FromMinutes(60), cancellationToken);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(true);

            if (!disposing)
            {
                return;
            }

            DisposeDataReceiver();
        }

        /// <summary>
        /// Method for when data is received. Used for adding a received folder onto the message queue
        /// when the association is closed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The progress update.</param>
        private void DataReceiver_DataReceived(object sender, DicomDataReceiverProgressEventArgs e)
        {
            var queueItem = new UploadQueueItem(
                                calledApplicationEntityTitle: e.DicomAssociation?.CalledAE,
                                callingApplicationEntityTitle: e.DicomAssociation?.CallingAE,
                                associationFolderPath: e.FolderPath,
                                rootDicomFolderPath: e.RootFolderPath,
                                associationGuid: e.AssociationId,
                                associationDateTime: e.SocketConnectionDateTime);

            if (e.ProgressCode == DicomReceiveProgressCode.AssociationReleased || e.ProgressCode == DicomReceiveProgressCode.TransferAborted)
            {
                // Send a log event
                LogInformation(LogEntry.Create(AssociationStatus.DicomAssociationClosed,
                                   uploadQueueItem: queueItem,
                                   dicomDataReceiverProgress: CreateReceiveProperties(e)));

                // If no data has been received, we do not need to add to the message queue.
                // An example of a no data received scenario is a Dicom echo.
                if (!e.AnyDataReceived)
                {
                    return;
                }

                using (var transaction = CreateQueueTransaction(_uploadQueuePath))
                {
                    BeginMessageQueueTransaction(transaction);

                    try
                    {
                        // Add the receive queue item onto the queue.
                        // No retry logic as if the method fails, retrying will not help
                        EnqueueMessage(queueItem, _uploadQueuePath, transaction);
                        transaction.Commit();
                    }
                    // This should never happen unless someone has manually changed the queue configuration
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        transaction.Abort();
                        LogError(LogEntry.Create(AssociationStatus.ReceiveEnqueueError, uploadQueueItem: queueItem),
                                 exception);
                    }
                }
            }
            else if (e.ProgressCode == DicomReceiveProgressCode.FileReceived
                || e.ProgressCode == DicomReceiveProgressCode.ConnectionClosed
                || e.ProgressCode == DicomReceiveProgressCode.AssociationEstablished)
            {
                LogInformation(LogEntry.Create(AssociationStatus.FileReceived,
                                   uploadQueueItem: queueItem,
                                   dicomDataReceiverProgress: CreateReceiveProperties(e)));
            }
            else if (e.ProgressCode == DicomReceiveProgressCode.AssociationEstablished)
            {
                LogInformation(LogEntry.Create(AssociationStatus.DicomAssociationOpened,
                                   uploadQueueItem: queueItem,
                                   dicomDataReceiverProgress: CreateReceiveProperties(e)));
            }
            else if (e.ProgressCode == DicomReceiveProgressCode.Echo)
            {
                LogInformation(LogEntry.Create(AssociationStatus.DicomEcho,
                                   uploadQueueItem: queueItem,
                                   dicomDataReceiverProgress: CreateReceiveProperties(e)));
            }
            else
            {
                LogError(LogEntry.Create(AssociationStatus.ReceiveUploadError, uploadQueueItem: queueItem,
                             dicomDataReceiverProgress: CreateReceiveProperties(e)),
                         new ReceiveServiceException("Cannot add to upload queue"));
            }
        }

        /// <summary>
        /// Disposes of the data receiver and sets the private value to null.
        /// </summary>
        private void DisposeDataReceiver()
        {
            if (_dataReceiver == null)
            {
                return;
            }

            _dataReceiver.DataReceived -= DataReceiver_DataReceived;

            _dataReceiver.Dispose();
            _dataReceiver = null;
        }

        /// <summary>
        /// Creates the receive properties for logging.
        /// </summary>
        /// <param name="e">The <see cref="DicomDataReceiverProgressEventArgs"/> instance containing the event data.</param>
        /// <returns>The receive properties.</returns>
        private static (object progressCode, string remoteHost, int remotePort, string uid, string version, string logPresentation) CreateReceiveProperties(DicomDataReceiverProgressEventArgs e) =>
            (e.ProgressCode, e.DicomAssociation.RemoteHost, e.DicomAssociation.RemotePort,
                e.DicomAssociation.RemoteImplementationClassUID?.UID, e.DicomAssociation.RemoteImplementationVersion,
                // This is causing too verbose logging. Consider turning back on if running a telemetry/logging solution that can deal with it
                //PresentationContextsToLogString(e.DicomAssociation.PresentationContexts)
                null);

        /// <summary>
        /// Converts a Dicom presentation context collection to a human readable string.
        /// </summary>
        /// <param name="presentationContextCollection">The presentation context collection.</param>
        /// <returns>The human readable string.</returns>
#pragma warning disable IDE0051 // Remove unused private members
        private static string PresentationContextsToLogString(DicomPresentationContextCollection presentationContextCollection)
#pragma warning restore IDE0051 // Remove unused private members
        {
            var stringBuilder = new StringBuilder();

            foreach (var item in presentationContextCollection)
            {
                stringBuilder.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "'{0}:{1}' ",
                        item.AbstractSyntax.Name,
                        item.AcceptedTransferSyntax?.UID.Name));
            }

            return stringBuilder.ToString();
        }
    }
}