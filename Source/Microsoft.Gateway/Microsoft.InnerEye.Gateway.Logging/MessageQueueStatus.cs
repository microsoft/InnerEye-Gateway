namespace Microsoft.InnerEye.Gateway.Logging
{
    /// <summary>
    /// The message queue item logging enumeration.
    /// </summary>
    public enum MessageQueueStatus
    {
        /// <summary>
        /// Initialised a message queue.
        /// </summary>
        InitialisedQueue,

        /// <summary>
        /// Error initialising message queue.
        /// </summary>
        InitialiseQueueError,

        /// <summary>
        /// Enqueued a message.
        /// </summary>
        Enqueued,

        /// <summary>
        /// Error enqueuing message.
        /// </summary>
        EnqueueError,

        /// <summary>
        /// Dequeued a message.
        /// </summary>
        Dequeued,

        /// <summary>
        /// Error dequeuing a message.
        /// </summary>
        DequeueError,

        /// <summary>
        /// Enqueued a message onto the dead letter queue.
        /// </summary>
        EnqueuedDeadLetter,

        /// <summary>
        /// Message too old to be put on dead letter queue.
        /// </summary>
        TooOldForDeadLetterError,

        /// <summary>
        /// Moved a message from one queue to another.
        /// </summary>
        Moved,

        /// <summary>
        /// Error moving message from one queue to another.
        /// </summary>
        MoveError,

        /// <summary>
        /// Error beginning transaction.
        /// </summary>
        BeginTransactionError,

        /// <summary>
        /// Error in handling a transaction exception.
        /// </summary>
        TransactionExceptionHandlerError,

        /// <summary>
        /// Error in Dispose.
        /// </summary>
        DisposeError,
    }
}