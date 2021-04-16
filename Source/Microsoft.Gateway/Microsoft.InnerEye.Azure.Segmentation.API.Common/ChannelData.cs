namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;

    using Dicom;

    /// <summary>
    /// Channel id and files
    /// </summary>
    public class ChannelData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelData"/> class.
        /// </summary>
        /// <param name="channelID">The channel identifier.</param>
        /// <param name="dicomFiles">The dicom files.</param>
        /// <exception cref="ArgumentNullException">
        /// channelId
        /// or
        /// dicomFiles
        /// </exception>
        public ChannelData(string channelID, IEnumerable<DicomFile> dicomFiles)
        {
            ChannelID = channelID ?? throw new ArgumentNullException(nameof(channelID));
            DicomFiles = dicomFiles ?? throw new ArgumentNullException(nameof(dicomFiles));
        }

        /// <summary>
        /// Gets the channel identifier.
        /// </summary>
        /// <value>
        /// The channel identifier.
        /// </value>
        public string ChannelID { get; }

        /// <summary>
        /// Gets the dicom files.
        /// </summary>
        /// <value>
        /// The dicom files.
        /// </value>
        public IEnumerable<DicomFile> DicomFiles { get; }
    }
}