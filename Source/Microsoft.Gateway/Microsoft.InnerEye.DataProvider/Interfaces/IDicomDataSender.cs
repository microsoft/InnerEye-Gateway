namespace Microsoft.InnerEye.Listener.DataProvider.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Dicom;

    using Microsoft.InnerEye.Listener.DataProvider.Models;

    /// <summary>
    /// Dicom data sender interface.
    /// </summary>
    public interface IDicomDataSender
    {
        /// <summary>
        /// Sends an echo request to the peer application entity.
        /// </summary>
        /// <param name="ownApplicationEntityTitle">Our own application entity title.</param>
        /// <param name="peerApplicationEntityTitle">The peer application entity title.</param>
        /// <param name="peerApplicationEntityPort">The peer application entity port.</param>
        /// <param name="peerApplicationEntityIPAddress">The peer application entity IP address.</param>
        /// <exception cref="ArgumentNullException">If the peerApplicationEntity is null.</exception>
        /// <exception cref="ArgumentException">
        /// peerApplicationEntity
        /// or
        /// ownApplicationEntityTitle
        /// </exception>
        /// <returns>The Dicom operation result.</returns>
        Task<DicomOperationResult> DicomEchoAsync(
            string ownApplicationEntityTitle,
            string peerApplicationEntityTitle,
            int peerApplicationEntityPort,
            string peerApplicationEntityIPAddress);

        /// <summary>
        /// Sends the Dicom files as a Dicom C-Store request.
        /// </summary>
        /// <param name="ownApplicationEntityTitle">The own application entity title.</param>
        /// <param name="peerApplicationEntityTitle">The peer application entity title.</param>
        /// <param name="peerApplicationEntityPort">The peer application entity port.</param>
        /// <param name="peerApplicationEntityIPAddress">The peer application entity IP address.</param>
        /// <param name="dicomFiles">The Dicom files to send.</param>
        /// <returns>The collection of Dicom files and the Dicom operation result for each item.</returns>
        Task<IEnumerable<Tuple<DicomFile, DicomOperationResult>>> SendFilesAsync(
            string ownApplicationEntityTitle,
            string peerApplicationEntityTitle,
            int peerApplicationEntityPort,
            string peerApplicationEntityIPAddress,
            params DicomFile[] dicomFiles);
    }
}