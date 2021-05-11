﻿namespace Microsoft.InnerEye.Listener.Common.Services
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for any exceptions in configuration.
    /// </summary>
    [Serializable]
    public class ServiceBaseException : Exception
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="ServiceBaseException"/> class from being created.
        /// </summary>
        private ServiceBaseException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBaseException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ServiceBaseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBaseException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ServiceBaseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBaseException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected ServiceBaseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
