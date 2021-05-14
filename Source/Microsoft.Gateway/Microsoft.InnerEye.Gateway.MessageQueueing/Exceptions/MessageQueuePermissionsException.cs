// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for any message queue permission errors e.g. no permission to read from a queue
    /// </summary>
    [Serializable]
    public class MessageQueuePermissionsException : Exception
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="MessageQueuePermissionsException"/> class from being created.
        /// </summary>
        private MessageQueuePermissionsException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueuePermissionsException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MessageQueuePermissionsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueuePermissionsException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public MessageQueuePermissionsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueuePermissionsException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected MessageQueuePermissionsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}