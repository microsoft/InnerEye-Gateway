namespace Microsoft.InnerEye.Listener.Processor.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
    using Microsoft.InnerEye.Gateway.Logging;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common.Services;

    using Newtonsoft.Json;

    /// <summary>
    /// The download service for downloading segmentation results.
    /// </summary>
    /// <seealso cref="SegmentationClientServiceBase" />
    public sealed class DownloadService : DequeueClientServiceBase<DownloadQueueItem>
    {
        /// <summary>
        /// The push queue path.
        /// </summary>
        private readonly string _pushQueuePath;

        /// <summary>
        /// The delete queue path.
        /// </summary>
        private readonly string _deleteQueuePath;

        /// <summary>
        /// Callback to get DownloadServiceConfig.
        /// </summary>
        private readonly Func<DownloadServiceConfig> _getDownloadServiceConfig;

        /// <summary>
        /// The maximum time the service will wait for a result to finish.
        /// </summary>
        private DownloadServiceConfig _downloadServiceConfig;

        /// <summary>
        /// Callback to create InnerEye segmentation client.
        /// </summary>
        private readonly Func<IInnerEyeSegmentationClient> _getInnerEyeSegmentationClient;

        /// <summary>
        /// The InnerEye segmentation client.
        /// </summary>
        private IInnerEyeSegmentationClient _innerEyeSegmentationClient;

        /// <summary>
        /// Create a new IMessageQueue for the delete queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue DeleteQueue => GatewayMessageQueue.Get(_deleteQueuePath);

        /// <summary>
        /// Create a new IMessageQueue for the download queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue DownloadQueue => DequeueMessageQueue;

        /// <summary>
        /// Create a new IMessageQueue for the push queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue PushQueue => GatewayMessageQueue.Get(_pushQueuePath);

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadService"/> class.
        /// </summary>
        /// <param name="getInnerEyeSegmentationClient">Callback to create InnerEye segmentation client.</param>
        /// <param name="downloadQueuePath">The download queue path.</param>
        /// <param name="pushQueuePath">The push queue path.</param>
        /// <param name="deleteQueuePath">The delete queue path.</param>
        /// <param name="getDownloadServiceConfig">Callback for download service config.</param>
        /// <param name="getDequeueServiceConfig">Callback for dequeue service config.</param>
        /// <param name="logger">The log.</param>
        /// <param name="instances">The number of concurrent execution instances we should have.</param>
        public DownloadService(
            Func<IInnerEyeSegmentationClient> getInnerEyeSegmentationClient,
            string downloadQueuePath,
            string pushQueuePath,
            string deleteQueuePath,
            Func<DownloadServiceConfig> getDownloadServiceConfig,
            Func<DequeueServiceConfig> getDequeueServiceConfig,
            ILogger logger,
            int instances)
            : base(getDequeueServiceConfig, downloadQueuePath, logger, instances)
        {
            _getInnerEyeSegmentationClient = getInnerEyeSegmentationClient ?? throw new ArgumentNullException(nameof(getInnerEyeSegmentationClient));
            _pushQueuePath = !string.IsNullOrWhiteSpace(pushQueuePath) ? pushQueuePath : throw new ArgumentException("The push queue path is null or white space.", nameof(pushQueuePath));
            _deleteQueuePath = !string.IsNullOrWhiteSpace(deleteQueuePath) ? deleteQueuePath : throw new ArgumentException("The delete queue path is null or white space.", nameof(deleteQueuePath));
            _getDownloadServiceConfig = getDownloadServiceConfig ?? throw new ArgumentNullException(nameof(getDownloadServiceConfig));
        }

        /// <inheritdoc/>
        protected override void OnServiceStart()
        {
            base.OnServiceStart();

            _downloadServiceConfig = _getDownloadServiceConfig();

            _innerEyeSegmentationClient?.Dispose();
            _innerEyeSegmentationClient = _getInnerEyeSegmentationClient.Invoke();
        }

        /// <summary>
        /// Called when [update tick asynchronous].
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The async task.</returns>
        protected override async Task OnUpdateTickAsync(CancellationToken cancellationToken)
        {
            using (var transaction = CreateQueueTransaction())
            {
                BeginMessageQueueTransaction(transaction);

                DownloadQueueItem queueItem = null;

                try
                {
                    queueItem = await DequeueNextMessageAsync(transaction, cancellationToken);

                    // Download the segmentation results
                    var segmentationResult = await GetSegmentationResultAsync(
                        queueItem,
                        _downloadServiceConfig.DownloadRetryTimespan,
                        _downloadServiceConfig.DownloadWaitTimeout,
                        cancellationToken);

                    // The result can be null if the API encountered an error. This is logged by the above method.
                    if (segmentationResult != null)
                    {
                        // Downloaded the segmentation result.
                        LogInformation(LogEntry.Create(AssociationStatus.Downloaded,
                                           downloadQueueItem: queueItem));

                        // Save the results to the result folder using the association Guid
                        await SaveDicomFilesAsync(queueItem.ResultsDirectory, segmentationResult);

                        // Enqueue the segmentation result onto the push queue only when we are not in dry run mode
                        // and we have something to push
                        if (!queueItem.IsDryRun)
                        {
                            EnqueueMessage(
                                new PushQueueItem(
                                    destinationApplicationEntity: queueItem.DestinationApplicationEntity,
                                    calledApplicationEntityTitle: queueItem.CalledApplicationEntityTitle,
                                    callingApplicationEntityTitle: queueItem.CallingApplicationEntityTitle,
                                    associationGuid: queueItem.AssociationGuid,
                                    associationDateTime: queueItem.AssociationDateTime,
                                    filePaths: Directory.EnumerateFiles(queueItem.ResultsDirectory).ToArray()),
                                _pushQueuePath,
                                transaction);
                        }
                    }

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
                catch (Exception e)
                {
                    LogError(LogEntry.Create(AssociationStatus.DownloadError, downloadQueueItem: queueItem),
                             e);

                    // If we can't process this item we may have data in the result directory; lets delete it.
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
        private void CleanUp(DownloadQueueItem queueItem, IQueueTransaction transaction)
        {
            if (queueItem == null || transaction == null || string.IsNullOrWhiteSpace(queueItem.ResultsDirectory))
            {
                return;
            }

            EnqueueMessage(
                new DeleteQueueItem(queueItem, queueItem.ResultsDirectory),
                _deleteQueuePath,
                transaction);
        }

        /// <summary>
        /// Opens a collection of Dicom file from a collection of binary arrays.
        /// </summary>
        /// <param name="file">The binary Dicom files.</param>
        /// <returns>The collection of Dicom file.</returns>
        private static IEnumerable<DicomFile> OpenDicomFiles(IEnumerable<byte[]> files)
        {
            foreach (var file in files)
            {
                using (var stream = new MemoryStream(file))
                {
                    yield return DicomFile.Open(stream);
                }
            }
        }

        /// <summary>
        /// Gets the segmentation result.
        /// </summary>
        /// <param name="downloadQueueItem">The download queue item.</param>
        /// <param name="retryDelay">The delay between getting segmentation progress.</param>
        /// <param name="timeout">The maximum time we will wait for a segmentation result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The segmentation result Dicom file or null.</returns>
        private async Task<DicomFile> GetSegmentationResultAsync(DownloadQueueItem downloadQueueItem, TimeSpan retryDelay, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var referenceDicomFiles = OpenDicomFiles(downloadQueueItem.ReferenceDicomFiles);

            // Attempting to download result
            LogInformation(LogEntry.Create(AssociationStatus.Downloading,
                               downloadQueueItem: downloadQueueItem,
                               downloadProgress: 0,
                               downloadError: string.Empty));

            var tagReplacements = JsonConvert.DeserializeObject<IEnumerable<TagReplacement>>(downloadQueueItem.TagReplacementJsonString);

            // Create a new token for the maximum time we will sit and wait for a result.
            // Note: We need to check both the passed cancellation token and this new token for cancellation requests
            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                while (!cancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    var modelResult = await _innerEyeSegmentationClient.SegmentationResultAsync(
                        modelId: downloadQueueItem.ModelId,
                        segmentationId: downloadQueueItem.SegmentationID,
                        referenceDicomFiles: referenceDicomFiles,
                        userReplacements: tagReplacements);

                    if (modelResult.Progress == 100 && modelResult.DicomResult != null)
                    {
                        return modelResult.DicomResult;
                    }
                    else if (!string.IsNullOrEmpty(modelResult.Error))
                    {
                        LogError(LogEntry.Create(AssociationStatus.Downloading,
                                     downloadQueueItem: downloadQueueItem,
                                     downloadProgress: modelResult.Progress,
                                     downloadError: modelResult.Error),
                                new ProcessorServiceException("Failed to get a segmentation result."));

                        // We cannot recover from this error, so we log and continue.
                        return null;
                    }
                    else
                    {
                        LogInformation(LogEntry.Create(AssociationStatus.Downloading,
                                           downloadQueueItem: downloadQueueItem,
                                           downloadProgress: modelResult.Progress,
                                           downloadError: modelResult.Error));
                    }

                    // Make sure you pass the cancellation token, not the timeout token, so the service can stop timely
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }

            return null;
        }

        /// <summary>
        /// Disposes of all managed resources.
        /// </summary>
        /// <param name="disposing">If we are disposing.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            _innerEyeSegmentationClient?.Dispose();
            _innerEyeSegmentationClient = null;
        }
    }
}
