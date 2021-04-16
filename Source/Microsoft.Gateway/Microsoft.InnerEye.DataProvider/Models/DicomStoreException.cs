namespace Microsoft.InnerEye.Listener.DataProvider.Models
{
    using System;
    using System.Runtime.Serialization;
    using Dicom.Network;

    /// <summary>
    /// Provide a meaningful message to the Store SCU when an error or warning occurs. .
    /// </summary>
    [Serializable]
    public class DicomStoreException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DicomStoreException()
            : base()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public DicomStoreException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomStoreException"/> class.
        /// </summary>
        /// <param name="serializationInfo">The serialization information.</param>
        /// <param name="streamingContext">The streaming context.</param>
        protected DicomStoreException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="exception">The exception.</param>
        public DicomStoreException(string message, Exception exception)
            : base(message, exception)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="status">The Dicom status.</param>
        /// <param name="message">The message.</param>
        public DicomStoreException(DicomStatus status, string message)
            : base(message)
        {
            Status = status;
        }

        /// <summary>
        /// Use the status field to inform the SCU in a meaningful way about the error or warning 
        /// that occurred. 
        /// </summary>
        public DicomStatus Status { get; private set; }

        /// <summary>
        /// Added an implementation of get object data for serializable class.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}