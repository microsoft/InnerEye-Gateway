// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// The queue item used for adding information onto the queue about files received over Dicom.
    /// </summary>
    [Serializable]
    public class UploadQueueItem : AssociationQueueItemBase
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="UploadQueueItem"/> class from being created.
        /// </summary>
        private UploadQueueItem()
            : base(
                  calledApplicationEntityTitle: string.Empty,
                  callingApplicationEntityTitle: string.Empty,
                  associationGuid: Guid.NewGuid(),
                  associationDateTime: DateTime.UtcNow,
                  dequeueCount: 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadQueueItem"/> class.
        /// </summary>
        /// <param name="calledApplicationEntityTitle">The original association called application entity title.</param>
        /// <param name="callingApplicationEntityTitle">The original association calling application entity title.</param>
        /// <param name="associationFolderPath">The folder path where the Dicom Data was stored.</param>
        /// <param name="rootDicomFolderPath">The root DICOM folder path.</param>
        /// <param name="remoteImplementationVersion">The remote implementation version.</param>
        /// <param name="remoteImplementationClassUID">The remote implementation class uid.</param>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The association date time.</param>
        public UploadQueueItem(
            string calledApplicationEntityTitle,
            string callingApplicationEntityTitle,
            string associationFolderPath,
            string rootDicomFolderPath,
            Guid associationGuid,
            DateTime associationDateTime)
            : this(
                  calledApplicationEntityTitle: calledApplicationEntityTitle,
                  callingApplicationEntityTitle: callingApplicationEntityTitle,
                  associationFolderPath: associationFolderPath,
                  rootDicomFolderPath: rootDicomFolderPath,
                  associationGuid: associationGuid,
                  associationDateTime: associationDateTime,
                  dequeueCount: 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadQueueItem"/> class.
        /// </summary>
        /// <param name="calledApplicationEntityTitle">The original association called application entity title.</param>
        /// <param name="callingApplicationEntityTitle">The original association calling application entity title.</param>
        /// <param name="associationFolderPath">The folder path where the Dicom Data was stored.</param>
        /// <param name="rootDicomFolderPath">The root DICOM folder path.</param>
        /// <param name="remoteImplementationVersion">The remote implementation version.</param>
        /// <param name="remoteImplementationClassUID">The remote implementation class uid.</param>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The association date time.</param>
        /// <param name="dequeueCount">The number of times this queue item has been dequeued.</param>
        [JsonConstructor]
        public UploadQueueItem(
            string calledApplicationEntityTitle,
            string callingApplicationEntityTitle,
            string associationFolderPath,
            string rootDicomFolderPath,
            Guid associationGuid,
            DateTime associationDateTime,
            int dequeueCount)
            : base(
                  calledApplicationEntityTitle,
                  callingApplicationEntityTitle,
                  associationGuid,
                  associationDateTime,
                  dequeueCount)
        {
            AssociationFolderPath = !string.IsNullOrWhiteSpace(associationFolderPath) ? associationFolderPath : throw new ArgumentException("associationFolderPath should be non-empty", nameof(associationFolderPath));
            RootDicomFolderPath = !string.IsNullOrWhiteSpace(rootDicomFolderPath) ? rootDicomFolderPath : throw new ArgumentException("rootDicomFolderPath should be non-empty", nameof(rootDicomFolderPath));
        }

        /// <summary>
        /// Gets the folder path where the association data was written to.
        /// </summary>
        /// <value>
        /// The folder path where the association data was written to.
        /// </value>
        public string AssociationFolderPath { get; }

        /// <summary>
        /// Gets the root dicom folder path.
        /// </summary>
        /// <value>
        /// The root dicom folder path.
        /// </value>
        public string RootDicomFolderPath { get; }
    }
}