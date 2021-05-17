// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.DataProvider
{
    using System;

    using Dicom;

    /// <summary>
    /// Dicom extension methods.
    /// </summary>
    public static class DicomExtensions
    {
        /// <summary>
        /// Return true if and only if the DicomDataset has the RT Structure set SOPClassUID
        /// </summary>
        /// <param name="dicomDataSet">The DICOM data set.</param>
        /// <returns>If the current dataset is an RT structure file.</returns>
        public static bool IsRTStructure(this DicomDataset dicomDataSet)
        {
            if (dicomDataSet == null)
            {
                throw new ArgumentNullException(nameof(dicomDataSet), "The Dicom data set is null");
            }

            return dicomDataSet.GetSingleValueOrDefault(
                DicomTag.SOPClassUID,
                new DicomUID(string.Empty, string.Empty, DicomUidType.Unknown)) == DicomUID.RTStructureSetStorage;
        }
    }
}