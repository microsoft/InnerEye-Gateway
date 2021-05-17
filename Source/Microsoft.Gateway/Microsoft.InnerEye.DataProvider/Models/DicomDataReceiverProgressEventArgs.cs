// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.DataProvider.Models
{
    using System;

    using Dicom.Network;

    using Microsoft.InnerEye.Listener.DataProvider.Interfaces;

    /// <summary>
    /// The Dicom data receiver update for an ongoing Dicom C-Store request.
    /// </summary>
    public class DicomDataReceiverProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the saver used for this progress event.
        /// </summary>
        /// <value>
        /// The saver used for this progress event.
        /// </value>
        private readonly IDicomSaver _dicomSaver;

        /// <summary>
        /// Construct a progress update for an ongoing Dicom C-Store request. 
        /// </summary>
        /// <param name="dicomImageSaver">The image saver used for this progress event.</param>
        /// <param name="progressCode">The progress code.</param>
        /// <param name="socketConnectionDateTime">The date time the socket connection started.</param>
        /// <param name="dicomAssociation">The Dicom association.</param>
        /// <param name="associationId">The Dicom association identifier.</param>
        public DicomDataReceiverProgressEventArgs(
            IDicomSaver dicomSaver,
            DicomReceiveProgressCode progressCode,
            DateTime socketConnectionDateTime,
            DicomAssociation dicomAssociation,
            Guid associationId)
        {
            _dicomSaver = dicomSaver ?? throw new ArgumentNullException(nameof(dicomSaver));

            ProgressCode = progressCode;
            SocketConnectionDateTime = socketConnectionDateTime;
            DicomAssociation = dicomAssociation;
            AssociationId = associationId;
        }

        /// <summary>
        /// Gets the current State of the association and store request.
        /// </summary>
        public DicomReceiveProgressCode ProgressCode { get; }

        /// <summary>
        /// Gets the date time the socket connection started.
        /// </summary>
        public DateTime SocketConnectionDateTime { get; }

        /// <summary>
        /// Gets the Dicom association
        /// </summary>
        public DicomAssociation DicomAssociation { get; }

        /// <summary>
        /// Gets the association identifier.
        /// </summary>
        /// <value>
        /// The association identifier.
        /// </value>
        public Guid AssociationId { get; }

        /// <summary>
        /// Gets the folder path where the association data was stored.
        /// </summary>
        /// <value>
        /// The folder for this association where all data was saved.
        /// </value>
        public string FolderPath => _dicomSaver.GetSaveFolderPath(AssociationId);

        /// <summary>
        /// Gets the root folder path for the Dicom saver.
        /// </summary>
        /// <value>
        /// The root folder where association folders will be created.
        /// </value>
        public string RootFolderPath => _dicomSaver.RootSaveFolder.FullName;

        /// <summary>
        /// Gets if any data has been received for this association.
        /// </summary>
        /// <value>
        /// True if any data has been received.
        /// </value>
        public bool AnyDataReceived => _dicomSaver.CheckIfAnyDataReceived(AssociationId);
    }
}