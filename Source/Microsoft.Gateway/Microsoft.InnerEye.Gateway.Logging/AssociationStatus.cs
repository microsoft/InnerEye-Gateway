namespace Microsoft.InnerEye.Gateway.Logging
{
    /// <summary>
    /// The DICOM association status logging enumeration.
    /// </summary>
    public enum AssociationStatus
    {
        /// <summary>
        /// The Gateway has received a Dicom echo request.
        /// </summary>
        DicomEcho,

        /// <summary>
        /// The Dicom association has closed.
        /// </summary>
        DicomAssociationClosed,

        /// <summary>
        /// A Dicom association has opened.
        /// </summary>
        DicomAssociationOpened,

        /// <summary>
        /// File received.
        /// </summary>
        FileReceived,

        /// <summary>
        /// The result of a check for patient consent.
        /// </summary>
        ConsentCheck,

        /// <summary>
        /// Files found that have been consented and can be uploaded for machine learning training.
        /// </summary>
        ConsentedUploadedFiles,

        /// <summary>
        /// Error enqueuing message.
        /// </summary>
        BaseEnqueueMessageError,

        /// <summary>
        /// Error enumerating directory.
        /// </summary>
        BaseEnumerateDirectoryError,

        /// <summary>
        /// Error opening DICOM file.
        /// </summary>
        BaseOpenDicomFileError,

        /// <summary>
        /// Uploading data to the API.
        /// </summary>
        Uploading,

        /// <summary>
        /// Finished uploading data to the API.
        /// </summary>
        Uploaded,

        /// <summary>
        /// Process an upload queue item.
        /// </summary>
        UploadProcessQueueItem,

        /// <summary>
        /// Error uploading data to the API.
        /// </summary>
        UploadError,

        /// <summary>
        /// Error in upload, the association folder has been deleted.
        /// </summary>
        UploadErrorAssocationFolderDeleted,

        /// <summary>
        /// Error in upload, no configuration matches the SOP class.
        /// </summary>
        UploadErrorMissingSopClassUploadConfiguration,

        /// <summary>
        /// Error in upload, tags do not match.
        /// </summary>
        UploadErrorTagsDoNotMatch,

        /// <summary>
        /// Error in upload, destination is null.
        /// </summary>
        UploadErrorDestinationEmpty,

        /// <summary>
        /// Error in upload, DICOM endpoint is not valid.
        /// </summary>
        UploadErrorDicomEndpointInvalid,

        /// <summary>
        /// Attempting to download data from the API.
        /// </summary>
        Downloading,

        /// <summary>
        /// Finished downloading data from the API.
        /// </summary>
        Downloaded,

        /// <summary>
        /// Error downloading data.
        /// </summary>
        DownloadError,

        /// <summary>
        /// Attempting a DICOM push for the downloaded result file.
        /// </summary>
        Pushing,

        /// <summary>
        /// DICOM push finished for the downloaded result file.
        /// </summary>
        Pushed,

        /// <summary>
        /// Error during DICOM push.
        /// </summary>
        PushError,

        /// <summary>
        /// Error, cannot push because applicationEntityConfig.Destination is null.
        /// </summary>
        PushErrorDestinationEmpty,

        /// <summary>
        /// Error in push, no files to push.
        /// </summary>
        PushErrorFilesEmpty,

        /// <summary>
        /// Delete path (either a directory or file).
        /// </summary>
        DeletePath,

        /// <summary>
        /// Deleted data from disk.
        /// </summary>
        Deleted,

        /// <summary>
        /// Error deleting data from disk.
        /// </summary>
        DeleteError,

        /// <summary>
        /// Posted the feedback structure set file.
        /// </summary>
        PostedFeedback,

        /// <summary>
        /// Uploaded DICOM series data for long term storage.
        /// </summary>
        UploadedDicomSeriesData,

        /// <summary>
        /// Error uploading DICOM series data.
        /// </summary>
        UploadDicomSeriesDataError,

        /// <summary>
        /// Receive service is starting.
        /// </summary>
        ReceiveServiceStart,

        /// <summary>
        /// An exception caught when enqueuing message.
        /// </summary>
        ReceiveEnqueueError,

        /// <summary>
        /// Error in receive, cannot add to upload queue.
        /// </summary>
        ReceiveUploadError,
    }
}