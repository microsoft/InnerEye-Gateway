namespace Microsoft.InnerEye.Listener.Receiver.Services
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for any exceptions in the receive service.
    /// </summary>
    [Serializable]
    public class ReceiveServiceException : Exception
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="ReceiveServiceException"/> class from being created.
        /// </summary>
        private ReceiveServiceException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveServiceException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ReceiveServiceException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveServiceException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ReceiveServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveServiceException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected ReceiveServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
