namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    /// <summary>
    /// The anonymisation method enumeration that defines what type of anonymisation should be applied to a DICOM tag.
    /// </summary>
    public enum AnonymisationMethod
    {
        /// <summary>
        /// If we wish to apply a one-way hash to the DICOM tag value.
        /// </summary>
        Hash,

        /// <summary>
        /// If we wish to keep the DICOM tag value.
        /// </summary>
        Keep,

        /// <summary>
        /// Randomises the date time DICOM tag with a date value between 0 -> 365 days and a time value
        /// between 0 -> 24 hours.
        /// </summary>
        RandomiseDateTime,
    }
}