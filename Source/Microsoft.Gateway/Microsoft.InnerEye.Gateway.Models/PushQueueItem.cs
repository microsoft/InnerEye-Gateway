namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The push queue item.
    /// </summary>
    /// <seealso cref="QueueItemBase" />
    [Serializable]
    public class PushQueueItem : AssociationQueueItemBase
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="DownloadQueueItem"/> class from being created.
        /// </summary>
        private PushQueueItem()
            : base(
                  calledApplicationEntityTitle: string.Empty,
                  callingApplicationEntityTitle: string.Empty,
                  associationGuid: Guid.NewGuid(),
                  associationDateTime: DateTime.UtcNow,
                  dequeueCount: 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushQueueItem"/> class.
        /// </summary>
        /// <param name="destinationApplicationEntity">The destination application entity.</param>
        /// <param name="calledApplicationEntityTitle">The original association called application entity title.</param>
        /// <param name="callingApplicationEntityTitle">The original association calling application entity title.</param>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The association date time.</param>
        /// <param name="filePaths">The collection of file paths that must be sent in the push.</param>
        public PushQueueItem(
            GatewayApplicationEntity destinationApplicationEntity,
            string calledApplicationEntityTitle,
            string callingApplicationEntityTitle,
            Guid associationGuid,
            DateTime associationDateTime,
            params string[] filePaths)
            : this(
                  destinationApplicationEntity: destinationApplicationEntity,
                  calledApplicationEntityTitle: calledApplicationEntityTitle,
                  callingApplicationEntityTitle: callingApplicationEntityTitle,
                  associationGuid: associationGuid,
                  associationDateTime: associationDateTime,
                  dequeueCount: 0,
                  filePaths: filePaths)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushQueueItem"/> class.
        /// </summary>
        /// <param name="destinationApplicationEntity">The destination application entity.</param>
        /// <param name="calledApplicationEntityTitle">The original association called application entity title.</param>
        /// <param name="callingApplicationEntityTitle">The original association calling application entity title.</param>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The association date time.</param>
        /// <param name="dequeueCount">The number of times this item has been dequeued.</param>
        /// <param name="filePaths">The collection of file paths that must be sent in the push.</param>
        [JsonConstructor]
        public PushQueueItem(
           GatewayApplicationEntity destinationApplicationEntity,
           string calledApplicationEntityTitle,
           string callingApplicationEntityTitle,
           Guid associationGuid,
           DateTime associationDateTime,
           int dequeueCount,
           params string[] filePaths)
           : base(
                 calledApplicationEntityTitle,
                 callingApplicationEntityTitle,
                 associationGuid,
                 associationDateTime,
                 dequeueCount)
        {
            DestinationApplicationEntity = destinationApplicationEntity;
            FilePaths = filePaths ?? new string[0];
        }

        /// <summary>
        /// Gets or sets the destination application entity title.
        /// </summary>
        /// <value>
        /// The destination application entity title.
        /// </value>
        public GatewayApplicationEntity DestinationApplicationEntity { get; set; }

        /// <summary>
        /// Gets the collection of file paths that must be sent in the push.
        /// </summary>
        /// <value>
        /// The collection of file paths that must be sent in the push.
        /// </value>
        public IEnumerable<string> FilePaths { get; }
    }
}