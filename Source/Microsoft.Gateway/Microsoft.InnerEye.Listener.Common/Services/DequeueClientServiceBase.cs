namespace Microsoft.InnerEye.Listener.Common.Services
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Logging;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;

    /// <summary>
    /// The service base for any service that will dequeue items from a message queue.
    /// </summary>
    /// <seealso cref="ThreadedServiceBase" />
    public abstract class DequeueClientServiceBase<T> : ThreadedServiceBase where T : QueueItemBase
    {
        /// <summary>
        /// Callback for gateway processor config.
        /// </summary>
        private Func<DequeueServiceConfig> _getDequeueServiceConfig;

        /// <summary>
        /// The dequeue queue path.
        /// </summary>
        private readonly string _dequeueQueuePath;

        /// <summary>
        /// The dead letter queue path.
        /// </summary>
        private readonly string _deadLetterQueuePath;

        /// <summary>
        /// Dequeue service config.
        /// </summary>
        private DequeueServiceConfig _dequeueServiceConfig;

        /// <summary>
        /// The last time the dead letter messages were moved from the dead letter queue to the dequeue queue.
        /// </summary>
        private DateTime _lastDeadLetterMove;

        /// <summary>
        /// Create a new IMessageQueue for the dead letter queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue DequeueMessageQueue => GatewayMessageQueue.Get(_dequeueQueuePath);

        /// <summary>
        /// Create a new IMessageQueue for the dead letter queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue DeadletterMessageQueue => GatewayMessageQueue.Get(_deadLetterQueuePath);

        /// <summary>
        /// Initializes a new instance of the <see cref="DequeueClientServiceBase"/> class.
        /// </summary>
        /// <param name="getDequeueServiceConfig">Callback for dequeue service config.</param>
        /// <param name="dequeueQueuePath">The dequeue queue path.</param>
        /// <param name="logger">The listener log.</param>
        /// <param name="instances">The number of concurrent execution instances we should have.</param>
        protected DequeueClientServiceBase(
            Func<DequeueServiceConfig> getDequeueServiceConfig,
            string dequeueQueuePath,
            ILogger logger,
            int instances)
            : base(logger, instances)
        {
            _getDequeueServiceConfig = getDequeueServiceConfig ?? throw new ArgumentNullException(nameof(getDequeueServiceConfig));

            // We create the dequeue and dead letter queue on construction. This can throw exceptions.
            _dequeueQueuePath = !string.IsNullOrWhiteSpace(dequeueQueuePath) ? dequeueQueuePath : throw new ArgumentException("The dequeue queue path is null or white space.", nameof(dequeueQueuePath));
            _deadLetterQueuePath = DequeueServiceConfig.DeadLetterQueuePath(dequeueQueuePath);
        }

        /// <summary>
        /// Creates a new queue transaction.
        /// </summary>
        /// <returns>The queue transaction.</returns>
        protected IQueueTransaction CreateQueueTransaction() => CreateQueueTransaction(_dequeueQueuePath);

        /// <summary>
        /// Called when the service is started.
        /// </summary>
        protected override void OnServiceStart()
        {
            _dequeueServiceConfig = _getDequeueServiceConfig();
        }

        /// <summary>
        /// Called when [service stop].
        /// </summary>
        protected override void OnServiceStop()
        {
        }

        /// <summary>
        /// Dequeues the next message from the queue. 
        /// Note: This method will not commit or abort the queue transaction.
        /// </summary>
        /// <param name="queueTransaction">The queue transaction.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The de-queued object.</returns>
        /// <exception cref="ArgumentException">If the queue path is not correct.</exception>
        /// <exception cref="MessageQueueReadException">The queue timed out when reading.</exception>
        /// <exception cref="MessageQueuePermissionsException">The queue does not have permissions to read.</exception>
        protected async Task<T> DequeueNextMessageAsync(
            IQueueTransaction queueTransaction,
            CancellationToken cancellationToken)
        {
            // Check if we should move all the dead letter messages on every dequeue.
            CheckMoveDeadLetterMessages();

            try
            {
                // Note: We do not put the message queue in a using block. The service manages the lifecycle
                // of message queues.
                var messageItem = GetMessageQueue(_dequeueQueuePath)
                                        .DequeueNextMessage<T>(queueTransaction);

                // Increase the count on the message.
                // Note: We should not check if the maximum dequeue count has been reached here. We should do that on handling exceptions.
                messageItem.DequeueCount++;

                LogInformation(LogEntry.Create(MessageQueueStatus.Dequeued,
                                   queueItemBase: messageItem,
                                   sourceMessageQueuePath: _dequeueQueuePath));

                return messageItem;
            }
            catch (MessageQueueReadException)
            {
                // Delay here before throw the exception up the next level to delay message queue reads.
                await Task.Delay(DequeueServiceConfig.DequeueTimeout, cancellationToken);
                throw;
            }
            catch (MessageQueuePermissionsException e)
            {
                LogError(LogEntry.Create(MessageQueueStatus.DequeueError, sourceMessageQueuePath: _dequeueQueuePath),
                         e);

                // We cannot recover from access violations. We should stop the service.
                StopServiceAsync();

                throw;
            }
        }

        /// <summary>
        /// Handles the intended behaviour when an unknown exception occurs when processing a queue item.
        /// We currently de-queue the item and put to the back of the queue. If we have attempted to process this
        /// item more than [X] times the item is removed from the queue and added to the dead letter queue (if the maximum age has not been reached).
        /// </summary>
        /// <param name="queueItem">The queue item.</param>
        /// <param name="queueTransaction">The message queue transaction.</param>
        /// <param name="oldQueueItemAction">Action when the queue item is old and will be removed from the queues (including the dead letter queue).</param>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2")]
        protected void HandleExceptionForTransaction(T queueItem, IQueueTransaction queueTransaction, Action oldQueueItemAction = null)
        {
            if (queueTransaction == null)
            {
                throw new ArgumentNullException(nameof(queueTransaction), "The queue transaction is null.");
            }

            // The queue item is null, commit the transaction.
            if (queueItem == null)
            {
                oldQueueItemAction?.Invoke();
                queueTransaction.Commit();

                return;
            }

            try
            {
                var isMessageOld = DateTime.UtcNow - queueItem.AssociationDateTime > _dequeueServiceConfig.MaximumQueueMessageAge;

                // If the item is not null and we haven't dequeued too many times, add to the queue at the back
                if (queueItem.DequeueCount < DequeueServiceConfig.MaxDequeueCount && !isMessageOld)
                {
                    EnqueueMessage(queueItem, _dequeueQueuePath, queueTransaction);
                }
                // Enqueue onto the dead letter queue if the message isn't old
                else if (!isMessageOld)
                {
                    EnqueueMessage(queueItem, _deadLetterQueuePath, queueTransaction);

                    LogInformation(LogEntry.Create(MessageQueueStatus.EnqueuedDeadLetter,
                                       queueItemBase: queueItem,
                                       sourceMessageQueuePath: _deadLetterQueuePath));
                }
                // Remove from all queues if the dequeue count has been reached and the message is old. Invoke any clean-up
                // tasks in the action.
                else
                {
                    // Make sure this is called before commiting the transaction. This will most likely use the transaction
                    // to enqueue to the delete service queue.
                    oldQueueItemAction?.Invoke();

                    LogError(LogEntry.Create(MessageQueueStatus.TooOldForDeadLetterError,
                                 queueItemBase: queueItem,
                                 sourceMessageQueuePath: _dequeueQueuePath),
                             new Exception("Message dequeued too many times. Remove from all queues."));
                }

                queueTransaction.Commit();
            }
            catch (Exception e)
            {
                LogError(LogEntry.Create(MessageQueueStatus.TransactionExceptionHandlerError,
                             queueItemBase: queueItem,
                             sourceMessageQueuePath: _dequeueQueuePath),
                         e);

                queueTransaction.Abort();
            }
        }

        /// <summary>
        /// Checks to see if we should move the dead letter messages back to the parent queue.
        /// </summary>
        /// <exception cref="ArgumentException">If the queue path is not correct.</exception>
        protected void CheckMoveDeadLetterMessages()
        {
            if (DateTime.UtcNow < _lastDeadLetterMove + _dequeueServiceConfig.DeadLetterMoveFrequency)
            {
                return;
            }

            // Move messages from dead letter queue to parent queue
            MoveQueueMessages(_deadLetterQueuePath, _dequeueQueuePath);

            _lastDeadLetterMove = DateTime.UtcNow;
        }

        /// <summary>
        /// Moves the queue messages from the source queue to the destination queue.
        /// </summary>
        /// <param name="sourceQueuePath">The source queue path.</param>
        /// <param name="destinationQueuePath">The destination queue path.</param>
        /// <exception cref="ArgumentException">If the queue path is not correct.</exception>
        private void MoveQueueMessages(string sourceQueuePath, string destinationQueuePath)
        {
            // Move messages from the source queue to the destination 
            var sourceQueue = GetMessageQueue(sourceQueuePath);
            var destinationQueue = GetMessageQueue(destinationQueuePath);

            var moreMessages = true;

            // Keep de-queueing until no more messages on the dead letter queue
            while (moreMessages)
            {
                // Create a new transaction for-each move request
                using (var transaction = CreateQueueTransaction(sourceQueuePath))
                {
                    transaction.Begin();

                    try
                    {
                        var message = sourceQueue.DequeueNextMessage<T>(transaction);

                        LogInformation(LogEntry.Create(MessageQueueStatus.Moved,
                                           queueItemBase: message,
                                           destinationMessageQueuePath: destinationQueuePath,
                                           sourceMessageQueuePath: sourceQueuePath));

                        destinationQueue.Enqueue(message, transaction);
                        transaction.Commit();
                    }
                    catch (MessageQueueReadException)
                    {
                        // No more messages
                        moreMessages = false;
                        transaction.Commit();
                    }
                    catch (MessageQueueWriteException e)
                    {
                        LogError(LogEntry.Create(MessageQueueStatus.MoveError,
                                     information: "Failed to wite a message from the dead letter queue to origin queue",
                                     destinationMessageQueuePath: destinationQueuePath,
                                     sourceMessageQueuePath: sourceQueuePath),
                                 e);
                        // Abort on a write exception
                        transaction.Abort();
                    }
                    catch (Exception e)
                    {
                        LogError(LogEntry.Create(MessageQueueStatus.MoveError,
                                     information: "Failed to dequeue a message from the dead letter queue",
                                     destinationMessageQueuePath: destinationQueuePath,
                                     sourceMessageQueuePath: sourceQueuePath),
                                 e);

                        transaction.Commit();
                    }
                }
            }
        }
    }
}