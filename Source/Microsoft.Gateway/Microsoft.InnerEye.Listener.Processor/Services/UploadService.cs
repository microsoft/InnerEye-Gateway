// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Processor.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
    using Microsoft.InnerEye.Listener.Common;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.InnerEye.Listener.Common.Services;
    using Microsoft.InnerEye.Listener.DataProvider.Models;

    using Newtonsoft.Json;

    /// <summary>
    /// Service for picking items of the message queue and sends data to the InnerEye cloud segmentation service.
    /// </summary>
    /// <seealso cref="SegmentationClientServiceBase" />
    public sealed class UploadService : DequeueClientServiceBase<UploadQueueItem>
    {
        /// <summary>
        /// The results folder.
        /// </summary>
        private const string ResultsFolder = "Results";

        /// <summary>
        /// The download queue path.
        /// </summary>
        private readonly string _downloadQueuePath;

        /// <summary>
        /// The delete queue path.
        /// </summary>
        private readonly string _deleteQueuePath;

        /// <summary>
        /// Callback to create InnerEye segmentation client.
        /// </summary>
        private readonly Func<IInnerEyeSegmentationClient> _getInnerEyeSegmentationClient;

        /// <summary>
        /// The InnerEye segmentation client.
        /// </summary>
        private IInnerEyeSegmentationClient _innerEyeSegmentationClient;

        /// <summary>
        /// AET configuration provider.
        /// </summary>
        private readonly Func<IEnumerable<AETConfigModel>> _aetConfigProvider;

        /// <summary>
        /// Cache AET configurations.
        /// </summary>
        private IEnumerable<AETConfigModel> _aetConfigModels;

        /// <summary>
        /// Create a new IMessageQueue for the delete queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue DeleteQueue => GatewayMessageQueue.Get(_deleteQueuePath);

        /// <summary>
        /// Create a new IMessageQueue for the download queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue DownloadQueue => GatewayMessageQueue.Get(_downloadQueuePath);

        /// <summary>
        /// Create a new IMessageQueue for the upload queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue UploadQueue => DequeueMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadService"/> class.
        /// </summary>
        /// <param name="getInnerEyeSegmentationClient">Callback to create InnerEye segmentation client.</param>
        /// <param name="aetConfigProvider">AET configuration provider.</param>
        /// <param name="uploadQueuePath">The upload queue path.</param>
        /// <param name="downloadQueuePath">The download queue path.</param>
        /// <param name="deleteQueuePath">The delete queue path.</param>
        /// <param name="getDequeueServiceConfig">Callback for dequeue service config.</param>
        /// <param name="logger">The log.</param>
        /// <param name="instances">The number of concurrent execution instances we should have.</param>
        public UploadService(
            Func<IInnerEyeSegmentationClient> getInnerEyeSegmentationClient,
            Func<IEnumerable<AETConfigModel>> aetConfigProvider,
            string uploadQueuePath,
            string downloadQueuePath,
            string deleteQueuePath,
            Func<DequeueServiceConfig> getDequeueServiceConfig,
            ILogger logger,
            int instances)
            : base(getDequeueServiceConfig, uploadQueuePath, logger, instances)
        {
            _getInnerEyeSegmentationClient = getInnerEyeSegmentationClient ?? throw new ArgumentNullException(nameof(getInnerEyeSegmentationClient));
            _aetConfigProvider = aetConfigProvider ?? throw new ArgumentNullException(nameof(aetConfigProvider));
            _downloadQueuePath = !string.IsNullOrWhiteSpace(downloadQueuePath) ? downloadQueuePath : throw new ArgumentException("The download path should not be null or whitespace.", nameof(downloadQueuePath));
            _deleteQueuePath = !string.IsNullOrWhiteSpace(deleteQueuePath) ? deleteQueuePath : throw new ArgumentException("The delete path should not be null or whitespace.", nameof(deleteQueuePath));
        }

        /// <inheritdoc/>
        protected override void OnServiceStart()
        {
            base.OnServiceStart();

            _innerEyeSegmentationClient?.Dispose();
            _innerEyeSegmentationClient = _getInnerEyeSegmentationClient.Invoke();

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

                UploadQueueItem queueItem = null;

                try
                {
                    queueItem = await DequeueNextMessageAsync(transaction, cancellationToken).ConfigureAwait(false);

                    // If the directory does not exist we cannot process this queue item.
                    // Lets log but remove this queue item.
                    if (Directory.Exists(queueItem.AssociationFolderPath))
                    {
                        await ProcessUploadQueueItem(queueItem, transaction).ConfigureAwait(false);

                        // Enqueue the message to delete the association folder
                        CleanUp(queueItem, transaction);
                    }
                    else
                    {
                        LogError(LogEntry.Create(AssociationStatus.UploadErrorAssocationFolderDeleted, uploadQueueItem: queueItem),
                                 new ProcessorServiceException("The association folder has been deleted."));
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
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    LogError(LogEntry.Create(AssociationStatus.UploadError, uploadQueueItem: queueItem),
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
        private void CleanUp(UploadQueueItem queueItem, IQueueTransaction transaction)
        {
            if (queueItem == null || transaction == null)
            {
                return;
            }

            // During a clean-up with remove everything in the association folder.
            EnqueueMessage(
                new DeleteQueueItem(queueItem, queueItem.AssociationFolderPath),
                _deleteQueuePath,
                transaction);
        }

        /// <summary>
        /// Uploads the files.
        /// </summary>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <param name="uploadQueueItem">The upload queue item.</param>
        /// <param name="queueTransaction">The queue transaction.</param>
        /// <returns>The async task.</returns>
        private async Task ProcessUploadQueueItem(UploadQueueItem uploadQueueItem, IQueueTransaction queueTransaction)
        {
            var clientConfiguration = ApplyAETModelConfigProvider.GetAETConfigs(
                _aetConfigModels,
                uploadQueueItem.CalledApplicationEntityTitle,
                uploadQueueItem.CallingApplicationEntityTitle);

            LogTrace(LogEntry.Create(AssociationStatus.UploadProcessQueueItem));

            switch (clientConfiguration.Config.AETConfigType)
            {
                // ML Model or ML Model with Dry Run Result
                case AETConfigType.Model:
                case AETConfigType.ModelWithResultDryRun:
                    // Load all DICOM files in the received folder.
                    var dicomFiles = ReadDicomFiles(uploadQueueItem.AssociationFolderPath, uploadQueueItem);
                    await ProcessModelConfig(dicomFiles, uploadQueueItem, queueTransaction, clientConfiguration).ConfigureAwait(false);

                    break;
                // ML Model dry run
                case AETConfigType.ModelDryRun:
                    // Anonymize and save the files locally for the dry run using the segmentation anonymisation protocol
                    await AnonymiseAndSaveDicomFilesAsync(
                        anonymisationProtocolId: _innerEyeSegmentationClient.SegmentationAnonymisationProtocolId,
                        anonymisationProtocol: _innerEyeSegmentationClient.SegmentationAnonymisationProtocol,
                        uploadQueueItem: uploadQueueItem,
                        aETConfigType: clientConfiguration.Config.AETConfigType).ConfigureAwait(false);

                    break;
            }
        }

        /// <summary>
        /// Anonymises every DICOM file in the specified folder path and saves locally to the
        /// correct dry run folder.
        /// </summary>
        /// <param name="anonymisationProtocolId">The anonymisation protocol unique identifier.</param>
        /// <param name="anonymisationProtocol">The anonymisation protocol.</param>
        /// <param name="uploadQueueItem">The upload queue item.</param>
        /// <param name="aETConfigType">Type of application entity title configuration.</param>
        /// <returns>The async task.</returns>
        private async Task AnonymiseAndSaveDicomFilesAsync(
            Guid anonymisationProtocolId,
            IEnumerable<DicomTagAnonymisation> anonymisationProtocol,
            UploadQueueItem uploadQueueItem,
            AETConfigType aETConfigType)
        {
            var dryRunFolder = DryRunFolders.GetFolder(aETConfigType);
            var resultFolderPath = Path.Combine(uploadQueueItem.RootDicomFolderPath, dryRunFolder, uploadQueueItem.AssociationGuid.ToString());

            foreach (var filePath in EnumerateFiles(uploadQueueItem.AssociationFolderPath, uploadQueueItem))
            {
                var dicomFile = TryOpenDicomFile(filePath, uploadQueueItem);

                // Not a DICOM file or is not a structure set file.
                if (dicomFile == null)
                {
                    EnqueueMessage(new DeleteQueueItem(uploadQueueItem, filePath), _deleteQueuePath);
                    continue;
                }

                var anonymizedDicomFile = _innerEyeSegmentationClient.AnonymizeDicomFile(
                    dicomFile: dicomFile,
                    anonymisationProtocolId: anonymisationProtocolId,
                    anonymisationProtocol: anonymisationProtocol);

                await SaveDicomFilesAsync(resultFolderPath, anonymizedDicomFile).ConfigureAwait(false);

                // This item has been saved, we can now delete this file
                EnqueueMessage(new DeleteQueueItem(uploadQueueItem, filePath), _deleteQueuePath);
            }
        }

        /// <summary>
        /// Processes the model configuration.
        /// </summary>
        /// <param name="uploadQueueItem">The upload queue item.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <param name="queueTransaction">The queue transaction.</param>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <returns>The waitable task.</returns>
        private async Task ProcessModelConfig(
            IEnumerable<DicomFile> dicomFiles,
            UploadQueueItem uploadQueueItem,
            IQueueTransaction queueTransaction,
            ClientAETConfig clientConfiguration)
        {
            var modelMatchResult = ApplyAETModelConfigProvider.ApplyAETModelConfig(clientConfiguration.Config.ModelsConfig, dicomFiles);

            if (modelMatchResult.Matched)
            {
                var model = modelMatchResult.Result;
                var queueItem = await StartSegmentationAsync(model.ChannelData, uploadQueueItem, model.ModelId, model.TagReplacements.ToArray(), clientConfiguration).ConfigureAwait(false);

                EnqueueMessage(queueItem, _downloadQueuePath, queueTransaction);
            }
            else
            {
                var failedDicomTags = modelMatchResult.GetDicomConstraintsDicomTags();

                // Log all the tags that did not match
                LogError(LogEntry.Create(AssociationStatus.UploadErrorTagsDoNotMatch,
                             uploadQueueItem: uploadQueueItem,
                             failedDicomTags: string.Join(",", failedDicomTags.Select(x => x.DictionaryEntry.Name))),
                         new ProcessorServiceException("Failed to find a model for the received Dicom data."));
            }
        }

        /// <summary>
        /// Gets the directory to store results from the download service.
        /// </summary>
        /// <param name="rootDicomFolder">The root Dicom folder.</param>
        /// <param name="isDryRun">If we should save the data to a dry run folder or results folder.</param>
        /// <returns>The results directory.</returns>
        private static string GetResultsDirectory(string rootDicomFolder, bool isDryRun)
        {
            return Directory.CreateDirectory(
                Path.Combine(
                    rootDicomFolder,
                    isDryRun ? DryRunFolders.DryRunModelWithResultFolder : ResultsFolder,
                    Guid.NewGuid().ToString())).FullName;
        }

        /// <summary>
        /// Copies all the data needed to be sent in the result association to the result directory.
        /// </summary>
        /// <param name="resultsDirectory">The result directory.</param>
        /// <param name="channelData">The machine learning model channel data.</param>
        /// <returns>The collection of file paths.</returns>
        private static void CopySendDataToResultsDirectory(string resultsDirectory, IEnumerable<ChannelData> channelData)
        {
            foreach (var channel in channelData)
            {
                foreach (var dicomFile in channel.DicomFiles)
                {
                    dicomFile.Save(Path.Combine(resultsDirectory, string.Format(CultureInfo.InvariantCulture, "{0}.dcm", Guid.NewGuid())));
                }
            }
        }

        /// <summary>
        /// Starts the segmentation task.
        /// </summary>
        /// <param name="channelData">The channel data.</param>
        /// <param name="uploadQueueItem">The upload queue item.</param>
        /// <param name="modelGuid">The model unique identifier.</param>
        /// <param name="tagReplacements">The tag replacements.</param>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <returns>The queue item.</returns>
        /// <exception cref="InvalidQueueDataException">If the result destination is not a valid AET.</exception>
        private async Task<DownloadQueueItem> StartSegmentationAsync
            (IEnumerable<ChannelData> channelData,
            UploadQueueItem uploadQueueItem,
            string modelGuid,
            TagReplacement[] tagReplacements,
            ClientAETConfig clientConfiguration)
        {
            if (clientConfiguration.Destination == null)
            {
                var exception = new ArgumentNullException("clientConfiguration.Destination",
                    "The result destination is null. The destination has not been configured.");

                LogError(LogEntry.Create(AssociationStatus.UploadErrorDestinationEmpty, uploadQueueItem: uploadQueueItem),
                         exception);

                throw exception;
            }

            // Validate the destination before uploading.
            ValidateDicomEndpoint(uploadQueueItem, clientConfiguration.Destination);

            LogInformation(LogEntry.Create(AssociationStatus.Uploading,
                               uploadQueueItem: uploadQueueItem,
                               modelId: modelGuid));

            var referenceDicomFiles = channelData.First().DicomFiles.ToArray();

            // Read all the bytes from the reference Dicom file and create new DICOM files with only the required DICOM tags
            var referenceDicomByteArrays = referenceDicomFiles
                                                    .CreateNewDicomFileWithoutPixelData(
                                                        _innerEyeSegmentationClient.SegmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));

            // Start the segmentation
            var (segmentationId, postedImages) = await _innerEyeSegmentationClient.StartSegmentationAsync(modelGuid, channelData).ConfigureAwait(false);

            LogInformation(LogEntry.Create(AssociationStatus.Uploaded,
                               uploadQueueItem: uploadQueueItem,
                               segmentationId: segmentationId,
                               modelId: modelGuid));

            var isDryRun = clientConfiguration.Config.AETConfigType == AETConfigType.ModelWithResultDryRun;
            var resultsDirectory = GetResultsDirectory(
                uploadQueueItem.RootDicomFolderPath,
                isDryRun: isDryRun);

            // Copy any data needed to be sent with result to results directory if needed.
            if (clientConfiguration.ShouldReturnImage)
            {
                CopySendDataToResultsDirectory(resultsDirectory, channelData);
            }

            return new DownloadQueueItem(
                segmentationId: segmentationId,
                modelId: modelGuid,
                resultsDirectory: resultsDirectory,
                referenceDicomFiles: referenceDicomByteArrays,
                calledApplicationEntityTitle: uploadQueueItem.CalledApplicationEntityTitle,
                callingApplicationEntityTitle: uploadQueueItem.CallingApplicationEntityTitle,
                destinationApplicationEntity: new GatewayApplicationEntity(
                                                    clientConfiguration.Destination.Title,
                                                    clientConfiguration.Destination.Port,
                                                    clientConfiguration.Destination.Ip),
                tagReplacementJsonString: JsonConvert.SerializeObject(tagReplacements),
                associationGuid: uploadQueueItem.AssociationGuid,
                associationDateTime: uploadQueueItem.AssociationDateTime,
                isDryRun: isDryRun);
        }

        /// <summary>
        /// Validates the DICOM endpoint.
        /// </summary>
        /// <param name="uploadQueueItem">Upload queue item.</param>
        /// <param name="dicomEndPoint">The DICOM endpoint to validate.</param>
        private void ValidateDicomEndpoint(UploadQueueItem uploadQueueItem, DicomEndPoint dicomEndPoint)
        {
            var titleValidationResult = ApplicationEntityValidationHelpers.ValidateTitle(dicomEndPoint.Title);
            var portValidationResult = ApplicationEntityValidationHelpers.ValidatePort(dicomEndPoint.Port);
            var ipValidationResult = ApplicationEntityValidationHelpers.ValidateIPAddress(dicomEndPoint.Ip);

            var result = titleValidationResult && portValidationResult && ipValidationResult;

            // Validate the destination before uploading.
            if (!result)
            {
                var exception = new ArgumentException("The DICOM endpoint is not valid.", nameof(dicomEndPoint));

                LogError(LogEntry.Create(AssociationStatus.UploadErrorDicomEndpointInvalid,
                            uploadQueueItem: uploadQueueItem,
                            destination: (ipAddress: dicomEndPoint.Ip, title: dicomEndPoint.Title, port: dicomEndPoint.Port)),
                         exception);

                // We cannot process message
                throw exception;
            }
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
