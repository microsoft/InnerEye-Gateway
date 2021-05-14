// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Processor.Services
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Logging;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common.Services;

    /// <summary>
    /// Deletes incoming data that has been processed
    /// </summary>
    /// <seealso cref="ThreadedServiceBase" />
    public sealed class DeleteService : DequeueClientServiceBase<DeleteQueueItem>
    {
        /// <summary>
        /// Create a new IMessageQueue for the delete queue.
        /// </summary>
        /// <returns>new IMessageQueue.</returns>
        public IMessageQueue DeleteQueue => DequeueMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteService"/> class.
        /// </summary>
        /// <param name="deleteQueuePath">The delete queue path.</param>
        /// <param name="getDequeueServiceConfig">Callback for dequeue service config.</param>
        /// <param name="logger">The log.</param>
        public DeleteService(
            string deleteQueuePath,
            Func<DequeueServiceConfig> getDequeueServiceConfig,
            ILogger logger)
            : base(getDequeueServiceConfig, deleteQueuePath, logger, 1) // We currently limit the delete service to one instance
        {
        }

        /// <summary>
        /// Called when [update tick] is called. This will wait for all work to execute then will pause for desired interval delay.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The async task.
        /// </returns>
        protected override async Task OnUpdateTickAsync(CancellationToken cancellationToken)
        {
            using (var transaction = CreateQueueTransaction())
            {
                BeginMessageQueueTransaction(transaction);

                DeleteQueueItem deleteQueueItem = null;

                try
                {
                    deleteQueueItem = await DequeueNextMessageAsync(transaction, cancellationToken).ConfigureAwait(false);

                    // Delete every path in the queue item. Each path could be a directory or a file.
                    foreach (var path in deleteQueueItem.Paths)
                    {
                        DeletePath(path);
                    }

                    LogInformation(LogEntry.Create(AssociationStatus.Deleted,
                                       deleteQueueItem: deleteQueueItem));

                    transaction.Commit();
                }
                catch (MessageQueueReadException)
                {
                    // We timed out trying to de-queue (no items on the queue). 
                    // This exception doesn't need to be logged.
                    transaction.Abort();
                }
                catch (OperationCanceledException)
                {
                    // Throw operation canceled exceptions up to the worker thread. It will handle
                    // logging correctly.
                    transaction.Abort();
                    throw;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    LogError(LogEntry.Create(AssociationStatus.DeleteError,
                                 deleteQueueItem: deleteQueueItem),
                             e);
                    HandleExceptionForTransaction(deleteQueueItem, transaction);
                }
            }
        }

        /// <summary>
        /// Deletes the path (either a directory or file).
        /// </summary>
        /// <param name="path">The path to delete.</param>
        private void DeletePath(string path)
        {
            LogTrace(LogEntry.Create(AssociationStatus.DeletePath, path: path));

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            else if (File.Exists(path))
            {
                var file = new FileInfo(path);
                file.Delete();

                var directory = file.Directory;

                // If the directory is empty, lets delete the folder.
                if (directory.GetFiles().Length == 0)
                {
                    // Throw errors on delete - in-case we are still attempting to write to this folder.
                    directory.Delete();
                }
            }
        }
    }
}