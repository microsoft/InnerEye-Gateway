// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// The Dicom association queue item.
    /// </summary>
    /// <seealso cref="QueueItemBase" />
    [Serializable]
    public class AssociationQueueItemBase : QueueItemBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssociationQueueItemBase"/> class.
        /// </summary>
        /// <param name="calledApplicationEntityTitle">The original association called application entity title.</param>
        /// <param name="callingApplicationEntityTitle">The original association calling application entity title.</param>
        /// <param name="associationGuid">The association unique identifier.</param>
        /// <param name="associationDateTime">The date time the association started.</param>
        /// <param name="dequeueCount">The number of times this queue item has been dequeued.</param>
        [JsonConstructor]
        public AssociationQueueItemBase(
            string calledApplicationEntityTitle,
            string callingApplicationEntityTitle,
            Guid associationGuid,
            DateTime associationDateTime,
            int dequeueCount)
            : base(
                  associationGuid: associationGuid,
                  associationDateTime: associationDateTime,
                  dequeueCount: dequeueCount)
        {
            CalledApplicationEntityTitle = calledApplicationEntityTitle;
            CallingApplicationEntityTitle = callingApplicationEntityTitle;
        }

        /// <summary>
        /// Gets the original Dicom association called application entity title.
        /// </summary>
        /// <value>
        /// The original Dicom association called application entity title.
        /// </value>
        public string CalledApplicationEntityTitle { get; }

        /// <summary>
        /// Gets the original Dicom association calling application entity.
        /// </summary>
        /// <value>
        /// The original Dicom association calling application entity.
        /// </value>
        public string CallingApplicationEntityTitle { get; }
    }
}