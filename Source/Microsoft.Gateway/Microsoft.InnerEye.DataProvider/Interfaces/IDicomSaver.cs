namespace Microsoft.InnerEye.Listener.DataProvider.Interfaces
{
    using System;
    using System.IO;

    using Dicom;
    using Dicom.Network;

    /// <summary>
    /// Interface for Dicom saving.
    /// </summary>
    public interface IDicomSaver
    {
        /// <summary>
        /// Gets the root folder where all data will be saved.
        /// </summary>
        /// <value>
        /// The root folder where all data will be saved.
        /// </value>
        DirectoryInfo RootSaveFolder { get; }

        /// <summary>
        /// Gets the folder path where images will be saved using the supplied directory name.
        /// </summary>
        /// <param name="associationId">The association identifier.</param>
        /// <returns>The save folder path.</returns>
        string GetSaveFolderPath(Guid associationId);

        /// <summary>
        /// Checks if any data has been received for the specific association.
        /// </summary>
        /// <param name="associationId">The association identifier.</param>
        /// <returns>True if any data has been received for the association.</returns>
        bool CheckIfAnyDataReceived(Guid associationId);

        /// <summary>
        /// This method saves incoming DICOM data according to implementation's logic.
        /// DicomStoreException thrown from this method will return its Status to the SCU and log the status. 
        /// Generic exceptions thrown from this method will return DicomStatus.ProcessingFailure to the SCU. 
        /// </summary>
        /// <param name="associationId">The association unique ID.</param>
        /// <param name="request">The Dicom C-Store request.</param>
        /// <param name="dicomFile">File to save</param>
        /// <returns>The file path where the image was saved.</returns>
        string SaveDicom(Guid associationId, DicomCStoreRequest request, DicomFile dicomFile);
    }
}