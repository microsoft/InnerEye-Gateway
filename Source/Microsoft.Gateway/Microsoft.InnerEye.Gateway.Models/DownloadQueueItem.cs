// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    /// <summary>
    /// Queue item used for downloading results from azure.
    /// </summary>
    [Serializable]
    public class DownloadQueueItem : AssociationQueueItemBase
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="DownloadQueueItem"/> class from being created.
        /// </summary>
        private DownloadQueueItem()
            : base(
                  calledApplicationEntityTitle: string.Empty,
                  callingApplicationEntityTitle: string.Empty,
                  associationGuid: Guid.NewGuid(),
                  associationDateTime: DateTime.UtcNow,
                  dequeueCount: 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadQueueItem"/> class.
        /// </summary>
        /// <param name="segmentationId">The segmentation unique identifier.</param>
        /// <param name="modelId">The model identifier.</param
        /// <param name="resultsDirectory">The directory to store all results.</param>
        /// <param name="referenceDicomFiles">The reference dicom files.</param>
        /// <param name="calledApplicationEntityTitle">The original association called application entity title.</param>
        /// <param name="callingApplicationEntityTitle">The original association calling application entity title.</param>
        /// <param name="destinationApplicationEntity">The destination application entity (can be null, and will be refreshed in the push service).</param>
        /// <param name="tagReplacementJsonString">The tag replacements as a Json string.</param>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The association date time.</param>
        /// <param name="isDryRun">If this is a dry run download, no push to destination.</param>
        public DownloadQueueItem(
            string segmentationId,
            string modelId,
            string resultsDirectory,
            IEnumerable<byte[]> referenceDicomFiles,
            string calledApplicationEntityTitle,
            string callingApplicationEntityTitle,
            GatewayApplicationEntity destinationApplicationEntity,
            string tagReplacementJsonString,
            Guid associationGuid,
            DateTime associationDateTime,
            bool isDryRun)
            : this(
                  segmentationId: segmentationId,
                  modelId: modelId,
                  resultsDirectory: resultsDirectory,
                  referenceDicomFiles: referenceDicomFiles,
                  calledApplicationEntityTitle: calledApplicationEntityTitle,
                  callingApplicationEntityTitle: callingApplicationEntityTitle,
                  destinationApplicationEntity: destinationApplicationEntity,
                  tagReplacementJsonString: tagReplacementJsonString,
                  associationGuid: associationGuid,
                  associationDateTime: associationDateTime,
                  dequeueCount: 0,
                  isDryRun: isDryRun)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadQueueItem"/> class.
        /// </summary>
        /// <param name="segmentationId">The segmentation unique identifier.</param>
        /// <param name="modelId">The model identifier.</param
        /// <param name="resultsDirectory">The directory to store all results.</param>
        /// <param name="referenceDicomFiles">The reference dicom files.</param>
        /// <param name="calledApplicationEntityTitle">The original association called application entity title.</param>
        /// <param name="callingApplicationEntityTitle">The original association calling application entity title.</param>
        /// <param name="destinationApplicationEntity">The destination application entity (can be null, and will be refreshed in the push service).</param>
        /// <param name="tagReplacementJsonString">The tag replacements as a Json string.</param>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The association date time.</param>
        /// <param name="dequeueCount">The number of times this queue item has been dequeued.</param>
        /// <param name="isDryRun">If this is a dry run download, no push to destination.</param>
        [JsonConstructor]
        public DownloadQueueItem(
            string segmentationId,
            string modelId,
            string resultsDirectory,
            IEnumerable<byte[]> referenceDicomFiles,
            string calledApplicationEntityTitle,
            string callingApplicationEntityTitle,
            GatewayApplicationEntity destinationApplicationEntity,
            string tagReplacementJsonString,
            Guid associationGuid,
            DateTime associationDateTime,
            int dequeueCount,
            bool isDryRun)
            : base(
                  calledApplicationEntityTitle,
                  callingApplicationEntityTitle,
                  associationGuid,
                  associationDateTime,
                  dequeueCount)
        {
            SegmentationID = segmentationId;
            ModelId = modelId;
            ResultsDirectory = !string.IsNullOrWhiteSpace(resultsDirectory) ? resultsDirectory : throw new ArgumentException(nameof(ResultsDirectory));
            ReferenceDicomFiles = referenceDicomFiles?.ToArray() ?? throw new ArgumentNullException(nameof(referenceDicomFiles));
            DestinationApplicationEntity = destinationApplicationEntity;
            TagReplacementJsonString = tagReplacementJsonString;
            IsDryRun = isDryRun;
        }

        /// <summary>
        /// Gets the download identifier for getting the result from the service.
        /// </summary>
        /// <value>
        /// The download identifier.
        /// </value>
        public string SegmentationID { get; }

        /// <summary>
        /// Gets the model identifier.
        /// </summary>
        /// <value>
        /// The model identifier.
        /// </value>
        public string ModelId { get; }

        /// <summary>
        /// Gets the dicom file to be used for de-anonymization.
        /// </summary>
        /// <value>
        /// The dicom file.
        /// </value>
        public IEnumerable<byte[]> ReferenceDicomFiles { get; }

        /// <summary>
        /// Gets the destination application entity title.
        /// </summary>
        /// <value>
        /// The destination application entity title.
        /// </value>
        public GatewayApplicationEntity DestinationApplicationEntity { get; }

        /// <summary>
        /// Gets the tag replacement json string.
        /// </summary>
        /// <value>
        /// The tag replacement json string.
        /// </value>
        public string TagReplacementJsonString { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is dry run.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is dry run; otherwise, <c>false</c>.
        /// </value>
        public bool IsDryRun { get; }

        /// <summary>
        /// Gets the directory to store all results.
        /// </summary>
        /// <value>
        /// The results directory.
        /// </value>
        public string ResultsDirectory { get; }
    }
}