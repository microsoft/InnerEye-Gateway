namespace Microsoft.InnerEye.Listener.DataProvider.Models
{
    using System;
    using System.Collections.Generic;

    using Dicom;
    using Dicom.Network;

    using Interfaces;

    /// <summary>
    /// Parameters to control the behaviour of and receive feedback from a Dicom File Store instance.
    /// </summary>
    public class DicomFileStoreParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DicomFileStoreParameters"/> class.
        /// </summary>
        /// <param name="update">The update.</param>
        /// <param name="getAcceptedTransferSyntaxes">
        /// The function for getting the accepted transfer syntaxes when an association is made.
        /// </param>
        /// <param name="dicomSaver">The Dicom saver.</param>
        /// <exception cref="NullReferenceException">
        /// Update or AcceptAssociation or ImageSaver
        /// </exception>
        public DicomFileStoreParameters(
            Action<Guid, DateTime, DicomAssociation, DicomReceiveProgressCode> update,
            Func<IReadOnlyDictionary<DicomUID, DicomTransferSyntax[]>> getAcceptedTransferSyntaxes,
            IDicomSaver dicomSaver)
        {
            Update = update ?? throw new ArgumentNullException(nameof(update), "The update action cannot be null.");
            GetAcceptedTransferSyntaxes = getAcceptedTransferSyntaxes ?? throw new ArgumentNullException(nameof(getAcceptedTransferSyntaxes), "The get accepted transfer syntaxes action cannot be null.");
            DicomSaver = dicomSaver ?? throw new ArgumentNullException(nameof(dicomSaver), "The DICOM saver cannot be null.");
        }

        /// <summary>
        /// The action will be called as data is received from the network peer.
        /// </summary>
        public Action<Guid, DateTime, DicomAssociation, DicomReceiveProgressCode> Update { get; }

        /// <summary>
        /// Gets the get accepted transfer syntaxes.
        /// </summary>
        /// <value>
        /// The get accepted transfer syntaxes.
        /// </value>
        public Func<IReadOnlyDictionary<DicomUID, DicomTransferSyntax[]>> GetAcceptedTransferSyntaxes { get; }

        /// <summary>
        /// Gets a DICOM saver that will be used to serialize accepted DicomFiles received
        /// from the network peer. 
        /// </summary>
        public IDicomSaver DicomSaver { get; }
    }
}