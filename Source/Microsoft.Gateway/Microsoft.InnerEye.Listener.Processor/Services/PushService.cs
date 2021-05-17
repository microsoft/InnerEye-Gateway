// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Processor.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Logging;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.InnerEye.Listener.Common.Services;
    using Microsoft.InnerEye.Listener.DataProvider.Interfaces;
    using Microsoft.InnerEye.Listener.DataProvider.Models;

    /// <summary>
    /// The push service.
    /// </summary>
    /// <seealso cref="ThreadedServiceBase" />
    public sealed class PushService : DequeueClientServiceBase<PushQueueItem>
    {
        /// <summary>
        /// The Dicom data sender.
        /// </summary>
        private readonly IDicomDataSender _dicomDataSender;

        /// <summary>
        /// AET configuration provider.
        /// </summary>
        private readonly Func<IEnumerable<AETConfigModel>> _aetConfigProvider;

        /// <summary>
        /// Cache AET configurations.
        /// </summary>
        private IEnumerable<AETConfigModel> _aetConfigModels;

        /// <summary>
        /// The delete queue path.
        /// </summary>
        private readonly string _deleteQueuePath;

        /// <summary>
        /// Create a new IMessageQueue for the delete queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue DeleteQueue => GatewayMessageQueue.Get(_deleteQueuePath);

        /// <summary>
        /// Create a new IMessageQueue for the push queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue PushQueue => DequeueMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushService"/> class.
        /// </summary>
        /// <param name="aetConfigProvider">AET configuration provider.</param>
        /// <param name="dicomDataSender">The Dicom data sender.</param>
        /// <param name="pushQueuePath">The push queue path.</param>
        /// <param name="deleteQueuePath">The delete queue path.</param>
        /// <param name="getDequeueServiceConfig">Callback for dequeue service config.</param>
        /// <param name="logger">The log.</param>
        /// <param name="instances">The instances.</param>
        public PushService(
            Func<IEnumerable<AETConfigModel>> aetConfigProvider,
            IDicomDataSender dicomDataSender,
            string pushQueuePath,
            string deleteQueuePath,
            Func<DequeueServiceConfig> getDequeueServiceConfig,
            ILogger logger,
            int instances)
            : base(getDequeueServiceConfig, pushQueuePath, logger, instances)
        {
            _aetConfigProvider = aetConfigProvider ?? throw new ArgumentNullException(nameof(aetConfigProvider));
            _deleteQueuePath = !string.IsNullOrWhiteSpace(deleteQueuePath) ? deleteQueuePath : throw new ArgumentException("The Queue path should not be null or whitespace.", nameof(deleteQueuePath));
            _dicomDataSender = dicomDataSender ?? throw new ArgumentNullException(nameof(dicomDataSender));
        }

        /// <inheritdoc/>
        protected override void OnServiceStart()
        {
            base.OnServiceStart();

            _aetConfigModels = _aetConfigProvider.Invoke();
        }

        /// <summary>
        /// Called when [update tick] is called. This will wait for all work to execute then will pause for desired interval delay.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The async task.
        /// </returns>
        protected override async Task OnUpdateTickAsync(CancellationToken cancellationToken)
        {
            using (var transaction = CreateQueueTransaction())
            {
                BeginMessageQueueTransaction(transaction);

                PushQueueItem queueItem = null;

                try
                {
                    queueItem = await DequeueNextMessageAsync(transaction, cancellationToken).ConfigureAwait(false);

                    // If we have dequed this item more than once, lets check if the destination 
                    // Dicom endpoint has been updated on the configuration service (or if the destination is null).
                    if (queueItem.DequeueCount > 1 || queueItem.DestinationApplicationEntity == null)
                    {
                        // Refresh the application entity config.
                        var applicationEntityConfig = ApplyAETModelConfigProvider.GetAETConfigs(
                            _aetConfigModels,
                            queueItem.CalledApplicationEntityTitle,
                            queueItem.CallingApplicationEntityTitle);

                        if (applicationEntityConfig.Destination == null)
                        {
                            var exception = new ArgumentNullException("applicationEntityConfig.Destination",
                                "The result destination is null. The destination has not been configured.");

                            LogError(LogEntry.Create(AssociationStatus.PushErrorDestinationEmpty, pushQueueItem: queueItem),
                                     exception);

                            throw exception;
                        }

                        queueItem.DestinationApplicationEntity = new GatewayApplicationEntity(
                            title: applicationEntityConfig.Destination.Title,
                            port: applicationEntityConfig.Destination.Port,
                            ipAddress: applicationEntityConfig.Destination.Ip);
                    }

                    if (queueItem.FilePaths.Any())
                    {
                        var dicomFiles = ReadDicomFiles(queueItem.FilePaths, queueItem);

                        await PushDicomFilesAsync(
                            pushQueueItem: queueItem,
                            ownApplicationEntityTitle: queueItem.CalledApplicationEntityTitle,
                            destination: queueItem.DestinationApplicationEntity,
                            cancellationToken: cancellationToken,
                            dicomFiles: dicomFiles.ToArray()).ConfigureAwait(false);
                    }

                    // Enqueue delete the files.
                    CleanUp(queueItem, transaction);

                    transaction.Commit();
                }
                catch (MessageQueueReadException)
                {
                    // We timed out trying to de-queue (no items on the queue). 
                    // This exception doesn't need to be logged.
                    transaction.Abort();
                }
                catch (OperationCanceledException)
                {
                    // Throw operation canceled exceptions up to the worker thread. It will handle
                    // logging correctly.
                    transaction.Abort();
                    throw;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    LogError(LogEntry.Create(AssociationStatus.PushError, pushQueueItem: queueItem),
                             e);

                    HandleExceptionForTransaction(
                        queueItem: queueItem,
                        queueTransaction: transaction,
                        oldQueueItemAction: () => CleanUp(queueItem, transaction));
                }
            }
        }

        /// <summary>
        /// The clean up action if the queue item has completed or failed and is now an old message.
        /// </summary>
        /// <param name="queueItem">The queue item.</param>
        /// <param name="transaction">The message queue transaction.</param>
        private void CleanUp(PushQueueItem queueItem, IQueueTransaction transaction)
        {
            if (queueItem == null || transaction == null)
            {
                return;
            }

            EnqueueMessage(
                new DeleteQueueItem(queueItem, queueItem.FilePaths),
                _deleteQueuePath,
                transaction);
        }

        /// <summary>
        /// Pushes the Dicom files to the destination application entity.
        /// </summary>
        /// <param name="pushQueueItem">The queue item.</param>
        /// <param name="ownApplicationEntityTitle">Our own application entity tile.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="dicomFiles">The Dicom files to push.</param>
        /// <returns>The async task.</returns>
        private async Task PushDicomFilesAsync(
            PushQueueItem pushQueueItem,
            string ownApplicationEntityTitle,
            GatewayApplicationEntity destination,
            CancellationToken cancellationToken,
            params DicomFile[] dicomFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            if (dicomFiles.Length == 0)
            {
                LogError(LogEntry.Create(AssociationStatus.PushErrorFilesEmpty, pushQueueItem: pushQueueItem),
                         new ProcessorServiceException("No files to push."));
                return;
            }

            LogInformation(LogEntry.Create(AssociationStatus.Pushing,
                               pushQueueItem: pushQueueItem,
                               destination: GetDestination(destination)));

            var result = await _dicomDataSender.SendFilesAsync(
                ownApplicationEntityTitle,
                destination.Title,
                destination.Port,
                destination.IpAddress,
                dicomFiles).ConfigureAwait(false);

            if (result.Any(x => x.Item2 != DicomOperationResult.Success))
            {
                throw new ArgumentException("Failed to push");
            }

            LogInformation(LogEntry.Create(AssociationStatus.Pushed,
                               pushQueueItem: pushQueueItem,
                               destination: GetDestination(destination)));
        }

        private static (string ipAddress, string title, int port) GetDestination(GatewayApplicationEntity gatewayApplicationEntity) =>
            (gatewayApplicationEntity.IpAddress, gatewayApplicationEntity.Title, gatewayApplicationEntity.Port);
    }
}