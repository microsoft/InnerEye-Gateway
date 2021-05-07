namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    /// <summary>
    /// The delete queue item.
    /// </summary>
    /// <seealso cref="QueueItemBase" />
    [Serializable]
    public class DeleteQueueItem : AssociationQueueItemBase
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="DeleteQueueItem"/> class from being created.
        /// </summary>
        private DeleteQueueItem()
            : base(
                  calledApplicationEntityTitle: string.Empty,
                  callingApplicationEntityTitle: string.Empty,
                  associationGuid: Guid.NewGuid(),
                  associationDateTime: DateTime.UtcNow,
                  dequeueCount: 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteQueueItem"/> class.
        /// </summary>
        /// <param name="associationQueueItemBase">The association queue item base.</param>
        /// <param name="paths">The paths to delete (can either be a directory or file path).</param>
        public DeleteQueueItem(
            AssociationQueueItemBase associationQueueItemBase,
            params string[] paths)
            : this(
                  associationQueueItemBase: associationQueueItemBase,
                  paths: paths.AsEnumerable())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteQueueItem"/> class.
        /// </summary>
        /// <param name="associationQueueItemBase">The association queue item base.</param>
        /// <param name="paths">The paths to delete (can either be a directory or file path).</param>
        public DeleteQueueItem(
            AssociationQueueItemBase associationQueueItemBase,
            IEnumerable<string> paths)
            : this(
                  calledApplicationEntityTitle: associationQueueItemBase.CalledApplicationEntityTitle,
                  callingApplicationEntityTitle: associationQueueItemBase.CallingApplicationEntityTitle,
                  paths: paths,
                  associationGuid: associationQueueItemBase.AssociationGuid,
                  // We reset the date time to maximise the amount of time we try to delete (this association could have expried and we are trying to clean up).
                  associationDateTime: DateTime.UtcNow,
                  dequeueCount: 0) // Default to zero
        {
        }

        /// <summary>
        /// Json constructor.
        /// </summary>
        /// <param name="paths">The paths to delete (can either be a directory or file path).</param>
        /// <param name="calledApplicationEntityTitle">The original association called application entity title.</param>
        /// <param name="callingApplicationEntityTitle">The original association calling application entity title.</param>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The association date time.</param>
        /// <param name="dequeueCount">The number of times this queue item has been dequeued.</param>
        [JsonConstructor]
        public DeleteQueueItem(
            IEnumerable<string> paths,
            string calledApplicationEntityTitle,
            string callingApplicationEntityTitle,
            Guid associationGuid,
            DateTime associationDateTime,
            int dequeueCount)
            : base(
                  calledApplicationEntityTitle: calledApplicationEntityTitle,
                  callingApplicationEntityTitle: callingApplicationEntityTitle,
                  associationGuid: associationGuid,
                  associationDateTime: associationDateTime,
                  dequeueCount: dequeueCount)
        {
            Paths = paths ?? Array.Empty<string>();
        }

        /// <summary>
        /// Get the file paths.
        /// </summary>
        /// <value>
        /// The file paths.
        /// </value>
        public IEnumerable<string> Paths { get; }
    }
}