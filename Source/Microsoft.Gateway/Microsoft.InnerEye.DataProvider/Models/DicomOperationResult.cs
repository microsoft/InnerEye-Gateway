namespace Microsoft.InnerEye.Listener.DataProvider.Models
{
    /// <summary>
    /// A Dicom operation result enumeration.
    /// </summary>
    public enum DicomOperationResult
    {
        /// <summary>
        /// If the operation correctly executed.
        /// </summary>
        Success,

        /// <summary>
        /// If the operation returned an error.
        /// </summary>
        Error,

        /// <summary>
        /// If the operation did not get a response from the receiver.
        /// </summary>
        NoResponse,
    }
}