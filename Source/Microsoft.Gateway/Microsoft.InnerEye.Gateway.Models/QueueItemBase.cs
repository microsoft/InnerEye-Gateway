// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// The queue item base.
    /// </summary>
    [Serializable]
    public class QueueItemBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueItemBase"/> class.
        /// </summary>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The date time the association started.</param>
        /// <param name="dequeueCount">The number of times this queue item has been dequeued.</param>
        [JsonConstructor]
        public QueueItemBase(Guid associationGuid, DateTime associationDateTime, int dequeueCount)
        {
            AssociationGuid = associationGuid;
            AssociationDateTime = associationDateTime;
            DequeueCount = dequeueCount;
        }

        /// <summary>
        /// Gets the association unique identifier.
        /// </summary>
        /// <value>
        /// The association unique identifier.
        /// </value>
        public Guid AssociationGuid { get; }

        /// <summary>
        /// Gets the date time when the Dicom association started.
        /// </summary>
        /// <value>
        /// Gets the date time when the Dicom association started.
        /// </value>
        public DateTime AssociationDateTime { get; }

        /// <summary>
        /// Gets or sets the dequeue count.
        /// </summary>
        /// <value>
        /// The dequeue count for this item.
        /// </value>
        public int DequeueCount { get; set; }
    }
}