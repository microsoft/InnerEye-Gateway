// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Logging
{
    /// <summary>
    /// Type of LogEntry.
    /// </summary>
    public enum LogEntryType
    {
        /// <summary>
        /// Log entry indicates ConfigurationService is starting.
        /// </summary>
        Initialize,

        /// <summary>
        /// Log entry indicates status of a DICOM association.
        /// The field <see cref="LogEntry.AssociationStatus"/> will be populated.
        /// <seealso cref="Logging.AssociationStatus"/>
        /// </summary>
        AssociationStatus,

        /// <summary>
        /// Log entry indicates status of a service.
        /// The field <see cref="LogEntry.ServiceStatus"/> will be populated.
        /// <seealso cref="Logging.ServiceStatus"/>
        /// </summary>
        ServiceStatus,

        /// <summary>
        /// Log entry indicated a message queue status.
        /// The field <see cref="LogEntry.MessageQueueStatus"/> will be populated.
        /// <seealso cref="Logging.MessageQueueStatus"/>
        /// </summary>
        MessageQueueStatus
    }
}
