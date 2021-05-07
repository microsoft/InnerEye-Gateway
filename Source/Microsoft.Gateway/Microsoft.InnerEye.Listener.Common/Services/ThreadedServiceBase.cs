namespace Microsoft.InnerEye.Listener.Common.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Logging;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;

    /// <summary>
    /// Base class for a service that can have multiple main worker loops (instances) that are executed on a separate task.
    /// </summary>
    /// <seealso cref="IService" />
    public abstract class ThreadedServiceBase : IService
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The message queues.
        /// </summary>
        private readonly Dictionary<string, IMessageQueue> _messageQueues = new Dictionary<string, IMessageQueue>();

        /// <summary>
        /// The execution tasks.
        /// </summary>
        private readonly Task[] _executionTasks;

        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// The start count. This is volatile as this property is mainly used for testing and will be accessed from a different task.
        /// </summary>
        private volatile int _startCount;

        /// <summary>
        /// If this instance is disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// If the main execution thread is running.
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// The software version string for appending to Dicom files the Gateway verison that modified/ saved the file.
        /// </summary>
        private readonly string _softwareVersionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadedServiceBase"/> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="instances">The number of concurrent execution instances we should have.</param>
        protected ThreadedServiceBase(
            ILogger logger,
            int instances)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "The log instance is null.");
            _executionTasks = new Task[instances];

            var softwareVersion = Assembly.GetExecutingAssembly().GetName().Version;
            _softwareVersionString = string.Format(CultureInfo.InvariantCulture, "Microsoft InnerEye Gateway: {0}", softwareVersion);
        }

        /// <summary>
        /// Called when the service wishes to stop.
        /// </summary>
        public event EventHandler<EventArgs> StopRequested;

        /// <summary>
        /// Gets a value indicating whether this instance is execution thread running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is execution thread running; otherwise, <c>false</c>.
        /// </value>
        public bool IsExecutionThreadRunning => _isRunning;

        /// <summary>
        /// Gets the number of times this instace has started.
        /// </summary>
        /// <value>
        /// The number of times this instance has started.
        /// </value>
        public int StartCount => _startCount;

        /// <summary>
        /// Starts the service.
        /// </summary>
        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                LogInformation(LogEntry.Create(ServiceStatus.Starting));

                // Start the service
                OnServiceStart();

                // Note: We should only start-up the worker task and return. We should not execute any long running operations here.
                for (var i = 0; i < _executionTasks.Length; i++)
                {
                    _executionTasks[i] = Task.Run(() => Execute(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
                    _startCount++;
                }

                _isRunning = true;

                LogInformation(LogEntry.Create(ServiceStatus.Started));
            }
            catch (Exception e)
            {
                LogError(LogEntry.Create(ServiceStatus.StartError), e);

                // Stop the service on any start-up failure
                StopServiceAsync();
            }
        }

        /// <summary>
        /// Called when the service is stopping.
        /// </summary>
        public void OnStop()
        {
            LogInformation(LogEntry.Create(ServiceStatus.Stopping));

            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                LogError(LogEntry.Create(ServiceStatus.StoppingError,
                             information: "Cancellation token source cancel exception."),
                         e);
            }

            try
            {
                // Will only wait 10 seconds for all tasks to finish nicely
                Task.WaitAll(_executionTasks.Where(x => x != null).ToArray(), TimeSpan.FromSeconds(10));
            }
            catch (Exception e)
            {
                LogError(LogEntry.Create(ServiceStatus.StoppingError,
                             information: "Unknown exception waiting for all execution tasks to end."),
                         e);
            }

            OnServiceStop();

            _isRunning = false;

            LogInformation(LogEntry.Create(ServiceStatus.Stopped));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of all managed resources.
        /// </summary>
        /// <param name="disposing">If we are disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
            {
                return;
            }

            OnStop();

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            foreach (var queue in _messageQueues)
            {
                try
                {
                    queue.Value.Dispose();
                }
                catch (Exception e)
                {
                    LogError(LogEntry.Create(MessageQueueStatus.DisposeError,
                                 sourceMessageQueuePath: queue.Value.QueuePath),
                             e);
                }
            }

            _messageQueues.Clear();

            _isDisposed = true;
        }

        /// <summary>
        /// Call when you would like this service instance to stop.
        /// </summary>
        protected void StopServiceAsync()
        {
            _cancellationTokenSource.Cancel();
            Task.Run(() => StopRequested?.Invoke(this, new EventArgs()));
        }

        /// <summary>
        /// Called when the service is started.
        /// </summary>
        protected abstract void OnServiceStart();

        /// <summary>
        /// Called when [service stop].
        /// </summary>
        protected abstract void OnServiceStop();

        /// <summary>
        /// Called when [update tick] is called. This will wait for all work to execute then will pause for desired interval delay.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The async task.</returns>
        protected abstract Task OnUpdateTickAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues the message using its own message queue transaction.
        /// </summary>
        /// <typeparam name="MessageType">The enqueue message type.</typeparam>
        /// <param name="message">The message to enqueue.</param>
        /// <param name="messageQueuePath">The queue path to enqueue the message onto.</param>
        protected void EnqueueMessage<TMessageType>(TMessageType message, string messageQueuePath) where TMessageType : QueueItemBase
        {
            using (var transaction = CreateQueueTransaction(messageQueuePath))
            {
                BeginMessageQueueTransaction(transaction);

                try
                {
                    EnqueueMessage(message, messageQueuePath, transaction);
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    LogError(LogEntry.Create(AssociationStatus.BaseEnqueueMessageError,
                                 queueItemBase: message),
                             e);
                    transaction.Abort();
                }
            }
        }

        /// <summary>
        /// Enqueues the message onto the message queue. 
        /// Note: This method will not commit or abort the queue transaction.
        /// </summary>
        /// <typeparam name="TMessageType">The enqueue message type.</typeparam>
        /// <param name="message">The message to enqueue.</param>
        /// <param name="messageQueuePath">The queue path to enqueue the message onto.</param>
        /// <param name="queueTransaction">The queue transaction.</param>
        /// <exception cref="ArgumentException">If the queue path is not correct.</exception>
        /// <exception cref="MessageQueuePermissionsException">If we do not have permissions to write to the message queue.</exception>
        /// <exception cref="MessageQueueWriteException">If the message queue could not write to the queue.</exception>
        protected void EnqueueMessage<TMessageType>(
            TMessageType message,
            string messageQueuePath,
            IQueueTransaction queueTransaction) where TMessageType : QueueItemBase
        {
            try
            {
                GetMessageQueue(messageQueuePath).Enqueue(message, queueTransaction);

                LogInformation(LogEntry.Create(MessageQueueStatus.Enqueued,
                                   queueItemBase: message,
                                   sourceMessageQueuePath: messageQueuePath));
            }
            catch (MessageQueuePermissionsException e)
            {
                LogError(LogEntry.Create(MessageQueueStatus.EnqueueError,
                             queueItemBase: message,
                             sourceMessageQueuePath: messageQueuePath),
                         e);

                // We cannot recover from access violations. We should stop the service.
                StopServiceAsync();

                throw;
            }
        }

        /// <summary>
        /// Begins the message queue transaction. 
        /// MessageQueueTransaction.Begin() can throw exceptions. This method wraps the retry logic.
        /// </summary>
        /// <param name="queueTransaction">The queue transaction.</param>
        /// <param name="retrySeconds">The time delay between every retry.</param>
        /// <param name="maximumRetry">The maximum number of retries.</param>
        /// <exception cref="InvalidOperationException">If the transaction has already been started.</exception>
        /// <exception cref="MessageQueueTransactionBeginException">If we failed to start a transaction after retrying.</exception>
        protected void BeginMessageQueueTransaction(IQueueTransaction queueTransaction, int retrySeconds = 30, int maximumRetry = 120)
        {
            if (queueTransaction == null)
            {
                throw new ArgumentNullException(nameof(queueTransaction), "The queue transaction is null.");
            }

            try
            {
                queueTransaction.Begin();
                return;
            }
            catch (MessageQueueTransactionBeginException e)
            {
                LogError(LogEntry.Create(MessageQueueStatus.BeginTransactionError),
                         e);

                // Throw if we reach the retry limit or cancellation has been requested (service stop)
                if (maximumRetry <= 0 || _cancellationTokenSource.IsCancellationRequested)
                {
                    throw;
                }
            }

            // Delay before retrying
            _cancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(retrySeconds));

            BeginMessageQueueTransaction(queueTransaction, retrySeconds, maximumRetry - 1);
        }

        /// <summary>
        /// Creates a new queue transaction.
        /// </summary>
        /// <param name="messageQueuePath">The queue path.</param>
        /// <returns>The queue transaction.</returns>
        protected IQueueTransaction CreateQueueTransaction(string messageQueuePath)
        {
            return GetMessageQueue(messageQueuePath).CreateQueueTransaction();
        }

        /// <summary>
        /// Gets a message queue or throws an exception.
        /// </summary>
        /// <param name="messageQueuePath">The message queue path.</param>
        /// <returns>The message queue.</returns>
        /// <exception cref="Exception">If any exception occured when getting the message queue.</exception>
        protected IMessageQueue GetMessageQueue(string messageQueuePath)
        {
            if (!_messageQueues.ContainsKey(messageQueuePath))
            {
                try
                {
                    var messageQueue = GatewayMessageQueue.Get(messageQueuePath);

                    LogInformation(LogEntry.Create(MessageQueueStatus.InitialisedQueue,
                                       sourceMessageQueuePath: messageQueuePath));

                    _messageQueues[messageQueuePath] = messageQueue;
                }
                catch (Exception e)
                {
                    LogError(LogEntry.Create(MessageQueueStatus.InitialiseQueueError,
                                 sourceMessageQueuePath: messageQueuePath),
                             e);

                    throw;
                }
            }

            return _messageQueues[messageQueuePath];
        }

        /// <summary>
        /// Attemps to save all the Dicom files to disk into the directory path with a new Guid per file.
        /// </summary>
        /// <param name="directory">The directory to save the files to disk.</param>
        /// <param name="dicomFiles">The Dicom files to write to disk.</param>
        /// <returns>The collection of saved file paths.</returns>
        /// <exception cref="ArgumentException">If the dicom files are null.</exception>
        /// <exception cref="IOException">If the folder path could not be created.</exception>
        protected Task<IEnumerable<string>> SaveDicomFilesAsync(string directory, params DicomFile[] dicomFiles)
        {
            return SaveDicomFilesAsync(directory, (IEnumerable<DicomFile>)dicomFiles);
        }

        /// <summary>
        /// Attemps to save all the Dicom files to disk into the directory path with a new Guid per file.
        /// </summary>
        /// <param name="directory">The directory to save the files to disk.</param>
        /// <param name="dicomFiles">The Dicom files to write to disk.</param>
        /// <returns>The collection of saved file paths.</returns>
        /// <exception cref="ArgumentException">If the dicom files are null.</exception>
        /// <exception cref="IOException">If the folder path could not be created.</exception>
        protected async Task<IEnumerable<string>> SaveDicomFilesAsync(string directory, IEnumerable<DicomFile> dicomFiles)
        {
            if (dicomFiles == null)
            {
                throw new ArgumentNullException(nameof(dicomFiles));
            }

            var filePaths = new List<string>();

            var directoryInfo = new DirectoryInfo(directory);

            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            foreach (var dicomFile in dicomFiles)
            {
                var filePath = $@"{directory}\{Guid.NewGuid()}.dcm";

                // Note: Whenever we save a file, we append our software version number.
                var newSoftwareVersions = new List<string>() { _softwareVersionString };
                if (dicomFile.Dataset.TryGetValues<string>(DicomTag.SoftwareVersions, out var oldValues))
                {
                    newSoftwareVersions.AddRange(oldValues);
                }

                dicomFile.Dataset.AddOrUpdate(DicomTag.SoftwareVersions, string.Join(@"\", newSoftwareVersions));

                await dicomFile.SaveAsync(filePath);

                filePaths.Add(filePath);
            }

            return filePaths;
        }

        /// <summary>
        /// Enumerates all files in a directory.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="queueItemBase">Queue item base.</param>
        /// <returns>The collection of file paths in a directory.</returns>
        protected IEnumerable<string> EnumerateFiles(string directoryPath, QueueItemBase queueItemBase)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("The directory path is null or white space.", nameof(directoryPath));
            }

            try
            {
                return Directory.EnumerateFiles(directoryPath);
            }
            catch (Exception e)
            {
                LogError(LogEntry.Create(AssociationStatus.BaseEnumerateDirectoryError,
                            queueItemBase: queueItemBase,
                            path: directoryPath),
                         e);
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Reads all dicom files in a directory.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="queueItemBase">Queue item base.</param>
        /// <returns>The collection of DICOM files.</returns>
        /// <exception cref="ArgumentException">directoryPath</exception>
        protected IEnumerable<DicomFile> ReadDicomFiles(string directoryPath, QueueItemBase queueItemBase)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("The directory path is null or white space.", nameof(directoryPath));
            }

            return ReadDicomFiles(EnumerateFiles(directoryPath, queueItemBase), queueItemBase);
        }

        /// <summary>
        /// Reads all the DICOM files from a collection of file paths.
        /// </summary>
        /// <param name="filePaths">The file paths.</param>
        /// <param name="queueItemBase">Queue item base.</param>
        /// <returns>The collection of DICOM files.</returns>
        protected IEnumerable<DicomFile> ReadDicomFiles(IEnumerable<string> filePaths, QueueItemBase queueItemBase)
        {
            foreach (var filePath in filePaths)
            {
                var dicomFile = TryOpenDicomFile(filePath, queueItemBase);

                if (dicomFile != null)
                {
                    yield return dicomFile;
                }
            }
        }

        /// <summary>
        /// Tries to open a dicom file and returns null if it fails.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="queueItemBase">Queue item base.</param>
        /// <returns>The DICOM file or null.</returns>
        protected DicomFile TryOpenDicomFile(string filePath, QueueItemBase queueItemBase)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            try
            {
                return DicomFile.Open(filePath);
            }
            catch (Exception e)
            {
                LogError(LogEntry.Create(AssociationStatus.BaseOpenDicomFileError,
                            queueItemBase: queueItemBase,
                            path: filePath),
                         e);
            }

            return null;
        }

        /// <summary>
        /// Entry point for the worker thread. This invokes the update method and pauses depending on the defined interval.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void Execute(CancellationToken cancellationToken)
        {
            // Main execution loop - if we encounter an exception during start-up, this will be false
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    OnUpdateTickAsync(cancellationToken).Wait(cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // If we got this error when the cancellation token has not been set, this needs to be logged.
                        // Otherwise, the service is shutting down normally and the wait has thrown an operation canceled exception.
                        LogError(LogEntry.Create(ServiceStatus.ExecuteError), e);
                    }
                }
                catch (Exception e)
                {
                    LogError(LogEntry.Create(ServiceStatus.ExecuteError), e);
                }
            }
        }

        /// <summary>
        /// Logs the event at information level.
        /// </summary>
        /// <param name="logEntry">Log entry.</param>
        protected void LogInformation(LogEntry logEntry) =>
            logEntry.Log(_logger, LogLevel.Information);

        /// <summary>
        /// Logs the event at trace level.
        /// </summary>
        /// <param name="logEntry">Log entry.</param>
        protected void LogTrace(LogEntry logEntry) =>
            logEntry.Log(_logger, LogLevel.Trace);

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="logEntry">Log entry.</param>
        /// <param name="exception">Exception.</param>
        protected void LogError(LogEntry logEntry, Exception exception) =>
            logEntry.Log(_logger, LogLevel.Error, exception);
    }
}
