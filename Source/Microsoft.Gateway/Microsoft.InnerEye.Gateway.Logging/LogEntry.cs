namespace Microsoft.InnerEye.Gateway.Logging
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Gateway.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Gateway log item.
    /// </summary>
    /// <remarks>
    /// This holds all properties that may be logged, but it is expected that in any instance
    /// almost all of them will be null.
    /// </remarks>
    public class LogEntry
    {
        /// <summary>
        /// Log entry type, <see cref="Logging.LogEntryType"/>.
        /// </summary>
        public LogEntryType LogEntryType { get; }

        /// <summary>
        /// Freeform additional information about log entry.
        /// </summary>
        public string Information { get; }

        /// <summary>
        /// Assocation status, populated if LogEntryType == LogEntryType.AssociationStatus.
        /// <seealso cref="Logging.AssociationStatus"/>
        /// </summary>
        public AssociationStatus? AssociationStatus { get; }

        /// <summary>
        /// Message queue status, populated if LogEntryType == LogEntryType.MessageQueueStatus.
        /// <seealso cref="Logging.MessageQueueStatus"/>
        /// </summary>
        public MessageQueueStatus? MessageQueueStatus { get; }

        /// <summary>
        /// Service status, populated if LogEntryType == LogEntryType.ServiceStatus.
        /// <seealso cref="Logging.ServiceStatus"/>
        /// </summary>
        public ServiceStatus? ServiceStatus { get; }

        /// <summary>
        /// DICOM association unique identifier, <see cref="Models.QueueItemBase.AssociationGuid"/>.
        /// </summary>
        public Guid? AssociationGuid { get; }

        /// <summary>
        /// Message queue dequeue count, <see cref="Models.QueueItemBase.DequeueCount"/>
        /// </summary>
        public int? MessageQueueDequeueCount { get; }

        /// <summary>
        /// DICOM association start time, <see cref="Models.QueueItemBase.AssociationDateTime"/>.
        /// </summary>
        public DateTime? AssociationDateTime { get; }

        /// <summary>
        /// Elapsed milliseconds, i.e. now - AssociationDateTime, in milliseconds.
        /// </summary>
        public double? ElapsedMilliseconds { get; }

        /// <summary>
        /// DICOM association calling application entity title, <see cref="Models.AssociationQueueItemBase.CallingApplicationEntityTitle"/>.
        /// </summary>
        public string CallingApplicationEntityTitle { get; }

        /// <summary>
        /// DICOM association called application entity title, <see cref="Models.AssociationQueueItemBase.CalledApplicationEntityTitle"/>.
        /// </summary>
        public string CalledApplicationEntityTitle { get; }

        /// <summary>
        /// Download segmentation identifier. Created during upload and copied to <see cref="Models.DownloadQueueItem.SegmentationID"/>.
        /// </summary>
        public string SegmentationId { get; }

        /// <summary>
        /// Segmentation model identifier, <see cref="Models.DownloadQueueItem.ModelId"/>.
        /// </summary>
        public string ModelId { get; }

        /// <summary>
        /// Segmentation download progress, from ModelResult.Progress (not imported here).
        /// </summary>
        public int? DownloadProgress { get; }

        /// <summary>
        /// Segmentation download error, from ModelResult.Error (not imported here).
        /// </summary>
        public string DownloadError { get; }

        /// <summary>
        /// Path to directory or file, will be one of: <see cref="Models.DeleteQueueItem.Paths"/> or
        /// <see cref="Models.PushQueueItem.FilePaths"/> or a file in the folder <see cref="Models.UploadQueueItem.AssociationFolderPath"/>.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Destination message queue path for moving a message between queues.
        /// </summary>
        public string DestinationMessageQueuePath { get; }

        /// <summary>
        /// Source message queue path for handling messages.
        /// </summary>
        public string SourceMessageQueuePath { get; }

        /// <summary>
        /// Destination IP address, <see cref="Models.PushQueueItem.DestinationApplicationEntity"/> or DicomEndPoint.Ip (not imported here).
        /// </summary>
        public string DestinationIp { get; }

        /// <summary>
        /// Destination title, <see cref="Models.PushQueueItem.DestinationApplicationEntity"/> or DicomEndPoint.Title (not imported here).
        /// </summary>
        public string DestinationTitle { get; }

        /// <summary>
        /// Destination port, <see cref="Models.PushQueueItem.DestinationApplicationEntity"/> or DicomEndPoint.Port (not imported here).
        /// </summary>
        public int? DestinationPort { get; }

        /// <summary>
        /// DICOM tags that did not match during upload.
        /// </summary>
        public string FailedTags { get; }

        /// <summary>
        /// DicomReceiveProgressCode from DicomDataReceiverProgressEventArgs (not imported here).
        /// </summary>
        public object ProgressCode { get; }

        /// <summary>
        /// DicomAssociation.RemoteHost from DicomDataReceiverProgressEventArgs (not imported here).
        /// </summary>
        public string CallingIp { get; }

        /// <summary>
        /// DicomAssociation.RemotePort from DicomDataReceiverProgressEventArgs (not imported here).
        /// </summary>
        public int? CallingPort { get; }

        /// <summary>
        /// DicomAssociation.RemoteImplementationClassUID?.UID from DicomDataReceiverProgressEventArgs (not imported here).
        /// </summary>
        public string RemoteImplementationClassUID { get; }

        /// <summary>
        /// DicomAssociation.RemoteImplementationVersion from DicomDataReceiverProgressEventArgs (not imported here).
        /// </summary>
        public string RemoteImplementationVersion { get; }

        /// <summary>
        /// Formatted DicomAssociation.PresentationContexts from DicomDataReceiverProgressEventArgs (not imported here).
        /// </summary>
        public string PresentationContexts { get; }

        /// <summary>
        /// Shorthand for creating an initialize log entry.
        /// </summary>
        /// <returns>New LogEntry of type Initialize.</returns>
        public static LogEntry CreateInitialize() =>
            new LogEntry(Logging.LogEntryType.Initialize);

        /// <summary>
        /// Shorthand for creating an association status log entry.
        /// </summary>
        /// <param name="associationStatus">Association status.</param>
        /// <param name="information">Freeform information about log item.</param>
        /// <param name="queueItemBase">Queue item base.</param>
        /// <param name="deleteQueueItem">Delete queue item.</param>
        /// <param name="downloadQueueItem">Download queue item.</param>
        /// <param name="downloadProgress">Download progress.</param>
        /// <param name="downloadError">Download error.</param>
        /// <param name="pushQueueItem">Push queue item.</param>
        /// <param name="uploadQueueItem">Upload queue item.</param>
        /// <param name="segmentationId">Segmentation id.</param>
        /// <param name="modelId">Model id.</param>
        /// <param name="destination">Destination.</param>
        /// <param name="path">Path.</param>
        /// <param name="failedDicomTags">String formatted list of failed DICOM tags.</param>
        /// <param name="dicomDataReceiverProgress">Receiver progress.</param>
        /// <returns>New LogEntry of type SegmentationStatus.</returns>
        public static LogEntry Create(
            AssociationStatus associationStatus,
            string information = null,
            QueueItemBase queueItemBase = null,
            DeleteQueueItem deleteQueueItem = null,
            DownloadQueueItem downloadQueueItem = null,
            int? downloadProgress = null,
            string downloadError = null,
            PushQueueItem pushQueueItem = null,
            UploadQueueItem uploadQueueItem = null,
            string segmentationId = null,
            string modelId = null,
            (string ipAddress, string title, int port)? destination = null,
            string path = null,
            string failedDicomTags = null,
            (object progressCode, string remoteHost, int remotePort, string uid, string version, string logPresentation)? dicomDataReceiverProgress = null) =>
                new LogEntry(
                    Logging.LogEntryType.AssociationStatus,
                    information: information,
                    associationStatus: associationStatus,
                    queueItemBase: queueItemBase,
                    deleteQueueItem: deleteQueueItem,
                    downloadQueueItem: downloadQueueItem,
                    downloadProgress: downloadProgress,
                    downloadError: downloadError,
                    pushQueueItem: pushQueueItem,
                    uploadQueueItem: uploadQueueItem,
                    segmentationId: segmentationId,
                    modelId: modelId,
                    destination: destination,
                    path: path,
                    failedDicomTags: failedDicomTags,
                    dicomDataReceiverProgress: dicomDataReceiverProgress);

        /// <summary>
        /// Shorthand for creating a service status log entry.
        /// </summary>
        /// <param name="serviceStatus">Service status.</param>
        /// <param name="information">Freeform information about log item.</param>
        /// <returns>New LogEntry of type ServiceStatus.</returns>
        public static LogEntry Create(
            ServiceStatus serviceStatus,
            string information = null) =>
                new LogEntry(Logging.LogEntryType.ServiceStatus, information: information, serviceStatus: serviceStatus);

        /// <summary>
        /// Shorthand for creating a message queue status log entry.
        /// </summary>
        /// <param name="messageQueueStatus">Message queue status.</param>
        /// <param name="information">Freeform information about log item.</param>
        /// <param name="queueItemBase">Queue item base.</param>
        /// <param name="destinationMessageQueuePath">Destination message queue path.</param>
        /// <param name="sourceMessageQueuePath">Source message queue path.</param>
        /// <returns>New LogEntry of type MessageQueueStatus.</returns>
        public static LogEntry Create(
            MessageQueueStatus messageQueueStatus,
            string information = null,
            QueueItemBase queueItemBase = null,
            string destinationMessageQueuePath = null,
            string sourceMessageQueuePath = null) =>
                new LogEntry(
                    Logging.LogEntryType.MessageQueueStatus,
                    information: information,
                    messageQueueStatus: messageQueueStatus,
                    queueItemBase: queueItemBase,
                    destinationMessageQueuePath: destinationMessageQueuePath,
                    sourceMessageQueuePath: sourceMessageQueuePath);

        /// <summary>
        /// Creates a new LogEntry.
        /// </summary>
        /// <param name="logEntryType">Log entry type.</param>
        /// <param name="information">Freeform information about log item.</param>
        /// <param name="associationStatus">Association status.</param>
        /// <param name="messageQueueStatus">Message queue status.</param>
        /// <param name="serviceStatus">Service status.</param>
        /// <param name="queueItemBase">Queue item base if derived type not known.</param>
        /// <param name="deleteQueueItem">Delete queue item.</param>
        /// <param name="downloadQueueItem">Download queue item.</param>
        /// <param name="downloadProgress">Download progress.</param>
        /// <param name="downloadError">Download error.</param>
        /// <param name="pushQueueItem">Push queue item.</param>
        /// <param name="uploadQueueItem">Upload queue item.</param>
        /// <param name="destinationMessageQueuePath">Destination message queue path.</param>
        /// <param name="sourceMessageQueuePath">Source message queue path.</param>
        /// <param name="segmentationId">Segmentation id.</param>
        /// <param name="modelId">Model id.</param>
        /// <param name="destination">Destination.</param>
        /// <param name="path">Path.</param>
        /// <param name="failedDicomTags">String formatted list of failed DICOM tags.</param>
        /// <param name="dicomDataReceiverProgress">Receiver progress.</param>
        public LogEntry(
            LogEntryType logEntryType,
            string information = null,
            AssociationStatus? associationStatus = null,
            MessageQueueStatus? messageQueueStatus = null,
            ServiceStatus? serviceStatus = null,
            QueueItemBase queueItemBase = null,
            DeleteQueueItem deleteQueueItem = null,
            DownloadQueueItem downloadQueueItem = null,
            int? downloadProgress = null,
            string downloadError = null,
            PushQueueItem pushQueueItem = null,
            UploadQueueItem uploadQueueItem = null,
            string destinationMessageQueuePath = null,
            string sourceMessageQueuePath = null,
            string segmentationId = null,
            string modelId = null,
            (string ipAddress, string title, int port)? destination = null,
            string path = null,
            string failedDicomTags = null,
            (object progressCode, string remoteHost, int remotePort, string uid, string version, string logPresentation)? dicomDataReceiverProgress = null)
        {
            LogEntryType = logEntryType;
            Information = information;
            AssociationStatus = associationStatus;
            MessageQueueStatus = messageQueueStatus;
            ServiceStatus = serviceStatus;

            var someAssociationQueueItemBase = deleteQueueItem ?? downloadQueueItem ?? (AssociationQueueItemBase)pushQueueItem ?? uploadQueueItem;
            var someQueueItemBase = queueItemBase ?? someAssociationQueueItemBase;

            if (someQueueItemBase != null)
            {
                AssociationGuid = someQueueItemBase.AssociationGuid;
                MessageQueueDequeueCount = someQueueItemBase.DequeueCount;
                AssociationDateTime = someQueueItemBase.AssociationDateTime;
                ElapsedMilliseconds = (DateTime.UtcNow - someQueueItemBase.AssociationDateTime).TotalMilliseconds;
            }

            if (someAssociationQueueItemBase != null)
            {
                CallingApplicationEntityTitle = someAssociationQueueItemBase.CallingApplicationEntityTitle;
                CalledApplicationEntityTitle = someAssociationQueueItemBase.CalledApplicationEntityTitle;
            }

            SegmentationId = downloadQueueItem?.SegmentationID ?? segmentationId;
            ModelId = downloadQueueItem?.ModelId ?? modelId;
            DownloadProgress = downloadProgress;
            DownloadError = downloadError;
            Path = uploadQueueItem?.AssociationFolderPath ?? path;
            DestinationMessageQueuePath = destinationMessageQueuePath;
            SourceMessageQueuePath = sourceMessageQueuePath;

            if (destination.HasValue)
            {
                var dest = destination.Value;

                DestinationIp = dest.ipAddress;
                DestinationTitle = dest.title;
                DestinationPort = dest.port;
            }

            FailedTags = failedDicomTags;

            if (dicomDataReceiverProgress.HasValue)
            {
                var progress = dicomDataReceiverProgress.Value;

                ProgressCode = progress.progressCode;
                CallingIp = progress.remoteHost;
                CallingPort = progress.remotePort;
                RemoteImplementationClassUID = progress.uid;
                RemoteImplementationVersion = progress.version;
                PresentationContexts = progress.logPresentation;
            }
        }

        /// <summary>
        /// Log an event and optional exception.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="logLevel">Log level.</param>
        /// <param name="exception">Optional exception.</param>
        public void Log(ILogger logger, Microsoft.Extensions.Logging.LogLevel logLevel, Exception exception = null) =>
            logger?.Log(logLevel, ToEventId(), this, exception, LogEntryFormatter);

        /// <summary>
        /// Create an EventId for a LogEntryType.
        /// </summary>
        /// <returns>New EventId.</returns>
        private EventId ToEventId() => new EventId((int)LogEntryType, LogEntryType.ToString());

        /// <summary>
        /// Format LogEntry and optional exception as string.
        /// </summary>
        /// <param name="logEntry">Log entry.</param>
        /// <param name="exception">Exception.</param>
        /// <returns>JSON stringified log entry.</returns>
        private static string LogEntryFormatter(LogEntry logEntry, Exception exception)
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                Converters = new[] { new StringEnumConverter() },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.SerializeObject(logEntry, serializerSettings);
        }
    }
}
