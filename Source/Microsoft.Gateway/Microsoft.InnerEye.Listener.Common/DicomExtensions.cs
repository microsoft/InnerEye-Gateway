// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Dicom;

    /// <summary>
    /// Dicom file extension methods.
    /// </summary>
    public static class DicomExtensions
    {
        /// <summary>
        /// The top level tags we want to keep.
        /// TODO: Remove from the Gateway and expose in the client SDK.
        /// </summary>
        private static readonly IEnumerable<DicomTag> _deAnonymizeTryAddReplaceAtTopLevel = new[]
        {
            // Patient module
            DicomTag.PatientID,
            DicomTag.PatientName,
            DicomTag.PatientBirthDate,
            DicomTag.PatientSex,

            // Study module
            DicomTag.StudyDate,
            DicomTag.StudyTime,
            DicomTag.ReferringPhysicianName,
            DicomTag.StudyID,
            DicomTag.AccessionNumber,
            DicomTag.StudyDescription,
        };

        /// <summary>
        /// Creates a new DICOM file using only the tags specified in the keep DICOM tags list and without the pixel data Tag.
        /// </summary>
        /// <param name="dicomFiles">The DICOM files to extract metadata from.</param>
        /// <returns>The DICOM files as a byte array with only the specified DICOM tags.</returns>
        /// <exception cref="ArgumentNullException">dicomFiles</exception>
        public static IEnumerable<byte[]> CreateNewDicomFileWithoutPixelData(this IEnumerable<DicomFile> dicomFiles, IEnumerable<DicomTag> keepDicomTags)
        {
            dicomFiles = dicomFiles ?? throw new ArgumentNullException(nameof(dicomFiles));
            keepDicomTags = keepDicomTags ?? throw new ArgumentNullException(nameof(keepDicomTags));

            foreach (var dicomFile in dicomFiles)
            {
                yield return dicomFile.CreateNewDicomFileWithoutPixelData(keepDicomTags);
            }
        }

        /// <summary>
        /// Creates a new DICOM file with the specified keep tags and without the pixel data Tag.
        /// </summary>
        /// <param name="dicomFile">The DICOM file.</param>
        /// <param name="keepDicomTags">The keep DICOM tags.</param>
        /// <returns>The DICOM file as a byte array with only the specified DICOM tags.</returns>
        /// <exception cref="ArgumentNullException">dicomFile</exception>
        public static byte[] CreateNewDicomFileWithoutPixelData(this DicomFile dicomFile, IEnumerable<DicomTag> keepDicomTags)
        {
            dicomFile = dicomFile ?? throw new ArgumentNullException(nameof(dicomFile));
            keepDicomTags = keepDicomTags.Concat(_deAnonymizeTryAddReplaceAtTopLevel) ?? throw new ArgumentNullException(nameof(keepDicomTags));

            var resultDataset = new List<DicomItem>();

            foreach (var dicomItem in dicomFile.Dataset)
            {
                if (dicomItem.Tag != DicomTag.PixelData && keepDicomTags.Contains(dicomItem.Tag))
                {
                    resultDataset.Add(dicomItem);
                }
            }

#pragma warning disable CS0618 // We need to skip validation since 
            var ds = new DicomDataset() { AutoValidate = false };
#pragma warning restore CS0618 // Type or member is obsolete
            ds.Add(resultDataset);

            var extractedDicomFile = new DicomFile(ds);

            using (var memoryStream = new MemoryStream())
            {
                extractedDicomFile.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}