namespace Microsoft.InnerEye.Gateway.MessageQueueing
{
    using System;

    /// <summary>
    /// A queue transaction interface.
    /// </summary>
    public interface IQueueTransaction : IDisposable
    {
        /// <summary>
        /// Begins the queue transaction.
        /// </summary>
        /// <exception cref="MessageQueueTransactionBeginException">Failed to begin the transaction.</exception>
        void Begin();

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Aborts the transaction.
        /// </summary>
        void Abort();
    }
}