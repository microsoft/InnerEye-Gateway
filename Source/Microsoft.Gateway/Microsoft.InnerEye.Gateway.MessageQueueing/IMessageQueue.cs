namespace Microsoft.InnerEye.Gateway.MessageQueueing
{
    using System;

    /// <summary>
    /// The message queue interface.
    /// </summary>
    public interface IMessageQueue : IDisposable
    {
        /// <summary>
        /// Gets the queue path.
        /// </summary>
        /// <value>
        /// The queue path.
        /// </value>
        string QueuePath { get; }

        /// <summary>
        /// Clear the entire queue of all messages.
        /// </summary>
        void Clear();

        /// <summary>
        /// Creates a new queue transaction.
        /// </summary>
        /// <returns>The queue transaction.</returns>
        IQueueTransaction CreateQueueTransaction();

        /// <summary>
        /// Dequeues the next message.
        /// </summary>
        /// <param name="queueTransaction">The queue transaction.</param>
        /// <returns>The next message on the queue.</returns>
        T DequeueNextMessage<T>(IQueueTransaction queueTransaction);

        /// <summary>
        /// Enqueues the specified message.
        /// </summary>
        /// <typeparam name="T">The enqueue type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="queueTransaction">The queue transaction.</param>
        void Enqueue<T>(T value, IQueueTransaction queueTransaction);
    }
}