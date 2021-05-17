// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.DataProvider.Implementations
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security;

    using Dicom;
    using Dicom.Network;

    using Microsoft.InnerEye.Listener.DataProvider.Interfaces;
    using Microsoft.InnerEye.Listener.DataProvider.Models;

    /// <summary>
    /// Dicom saver implementation for saving files to folders based on the association unique ID.
    /// </summary>
    public class ListenerDicomSaver : IDicomSaver
    {
        /// <summary>
        /// Lock object for locking when creating folders.
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>
        /// Constructor - this will throw exceptions if we cannot get or create the root folder
        /// or we do not have write access to the root folder.
        /// </summary>
        /// <param name="rootFolder">The root folder for saving images.</param>
        /// <exception cref="ArgumentException">If the root folder is null or white space..</exception>
        /// <exception cref="SecurityException">If we do not have permissions to create the root folder or sub folders.</exception>
        public ListenerDicomSaver(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
            {
                throw new ArgumentException("The root folder is null or white space.", nameof(rootFolder));
            }

            var directoryInformation = GetOrCreateDirectory(rootFolder);

            if (!directoryInformation.Exists)
            {
                throw new SecurityException(FormatLogStatement("The directory does not exist or could not be created."));
            }

            RootSaveFolder = directoryInformation;

            // Attempt to create a folder in the root folder.
            // If we do not have permission the test directory will not be created.
            var testDirectoryInformation = GetOrCreateSaveFolder(Guid.Empty);

            if (!testDirectoryInformation.Exists)
            {
                throw new SecurityException(
                    FormatLogStatement(string.Format(
                        CultureInfo.InvariantCulture,
                        "The process does not have the required permissions to create folders in {0}.",
                        rootFolder)));
            }

            testDirectoryInformation.Delete();
        }

        /// <summary>
        /// Gets the root folder where all data will be saved.
        /// </summary>
        /// <value>
        /// The root folder where all data will be saved.
        /// </value>
        public DirectoryInfo RootSaveFolder { get; }

        /// <summary>
        /// Gets the folder path where images will be saved using the supplied directory name.
        /// </summary>
        /// <param name="associationId">The association identifier.</param>
        /// <returns>The save folder path.</returns>
        public string GetSaveFolderPath(Guid associationId) => Path.Combine(RootSaveFolder.FullName, associationId.ToString());

        /// <summary>
        /// Checks if any data has been received for the specific association identifier.
        /// </summary>
        /// <param name="associationId">The association identifier.</param>
        /// <returns>True if any data has been received for this association.</returns>
        public bool CheckIfAnyDataReceived(Guid associationId)
        {
            var directoryInfo = new DirectoryInfo(GetSaveFolderPath(associationId));

            return directoryInfo.Exists && directoryInfo.EnumerateFiles().Any();
        }

        /// <summary>
        /// This method saves incoming DICOM image according to implementation's logic.
        /// DicomStoreException thrown from this method will return its Status to the SCU and log the status. 
        /// Generic exceptions thrown from this method will return DicomStatus.ProcessingFailure to the SCU. 
        /// </summary>
        /// <param name="associationId">The association unique ID.</param>
        /// <param name="request">The Dicom C-Store request.</param>
        /// <param name="dicomFile">File to save</param>
        /// <returns>The file path where the image was saved.</returns>
        public string SaveDicom(Guid associationId, DicomCStoreRequest request, DicomFile dicomFile)
        {
            if (dicomFile == null || dicomFile.Dataset == null)
            {
                throw new ArgumentNullException(nameof(dicomFile), FormatLogStatement("This Dicom file or dataset cannot be null."));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var sopInstanceUID = request.SOPInstanceUID.UID;

            if (string.IsNullOrWhiteSpace(sopInstanceUID))
            {
                throw new DicomStoreException(
                    DicomStatus.MissingAttributeValue,
                    FormatLogStatement("We cannot save a file without a SOP instance UID"));
            }

            var saveFolder = GetOrCreateSaveFolder(associationId);

            if (!saveFolder.Exists)
            {
                // This will never be a permissions failure as this is checked when this class is constructed.
                // If we fail this will be because we have run out of space on disk.
                throw new DicomStoreException(
                    DicomStatus.StorageStorageOutOfResources,
                    FormatLogStatement(string.Format(
                        CultureInfo.InvariantCulture,
                        "An exception occurred trying to get or create the folder path {0}",
                        saveFolder)));
            }

            var saveFilePath = Path.Combine(saveFolder.FullName, string.Format(CultureInfo.InvariantCulture, "{0}.dcm", sopInstanceUID));

            try
            {
                dicomFile.Save(saveFilePath);
            }
            catch (Exception e)
            {
                Trace.TraceError(FormatLogStatement(string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed to save the Dicom file to {0} with exception {1}",
                    saveFilePath,
                    e)));

                throw new DicomStoreException(DicomStatus.ProcessingFailure, e.ToString());
            }

            return saveFilePath;
        }

        /// <summary>
        /// Attempts to get or create the save folder for the association identifier.
        /// </summary>
        /// <param name="associationId">The association identifier.</param>
        /// <returns>The save folder directory information.</returns>
        private DirectoryInfo GetOrCreateSaveFolder(Guid associationId)
        {
            var saveFolderPath = GetSaveFolderPath(associationId);
            return GetOrCreateDirectory(saveFolderPath);
        }

        /// <summary>
        /// Attempts to get or create (if the folder does not exist) the specified folder path.
        /// </summary>
        /// <param name="folderPath">The folder path to get or create.</param>
        /// <returns>The folder directory information.</returns>
        private DirectoryInfo GetOrCreateDirectory(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("The folder path is null or white space.", nameof(folderPath));
            }

            return GetOrCreateDirectory(new DirectoryInfo(folderPath));
        }

        /// <summary>
        /// Attempts to get or create a folder (if the folder does not exist) using the input directory information.
        /// </summary>
        /// <param name="directoryInfo">The directory information for the folder to get or create.</param>
        /// <returns>The directory information.</returns>
        private DirectoryInfo GetOrCreateDirectory(DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
            {
                throw new ArgumentNullException(nameof(directoryInfo));
            }

            try
            {
                // We create directories under lock, as we may have multiple events trying to save at the same time
                lock (_lockObject)
                {
                    if (!directoryInfo.Exists)
                    {
                        directoryInfo = Directory.CreateDirectory(directoryInfo.FullName);
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Trace.TraceError(FormatLogStatement(string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed to create directory {0} with exception {1}",
                    directoryInfo.FullName,
                    e)));
            }

            return directoryInfo;
        }

        /// <summary>
        /// Gets for formatted statement for logging.
        /// </summary>
        /// <param name="value">The inner statement.</param>
        /// <returns>The formatted log statement.</returns>
        private string FormatLogStatement(string value) =>
                string.Format(CultureInfo.InvariantCulture, "[{0}] {1}", GetType().Name, value);
    }
}