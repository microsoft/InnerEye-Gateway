// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// The DICOM file information.
    /// </summary>
    [Serializable]
    public class DicomFileInformation
    {
        /// <summary>
        /// The pre-computed hash code of this instance.
        /// </summary>
        private readonly int _hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomFileInformation"/> class.
        /// </summary>
        /// <param name="patientId">The patient identifier.</param>
        /// <param name="studyInstanceUid">The study instance unique identifier.</param>
        /// <param name="seriesInstanceUid">The series instance unique identifier.</param>
        /// <param name="dicomModality">The modality of the Dicom object this file information represents.</param>
        [JsonConstructor]
        public DicomFileInformation(string patientId, string studyInstanceUid, string seriesInstanceUid, string dicomModality)
        {
            PatientId = string.IsNullOrWhiteSpace(patientId) ? throw new ArgumentException("patientId should be non-empty", nameof(patientId)) : patientId;
            StudyInstanceUid = string.IsNullOrWhiteSpace(studyInstanceUid) ? throw new ArgumentException("studyInstanceUid should be non-empty", nameof(studyInstanceUid)) : studyInstanceUid;
            SeriesInstanceUid = string.IsNullOrWhiteSpace(seriesInstanceUid) ? throw new ArgumentException("seriesInstanceUid should be non-empty", nameof(seriesInstanceUid)) : seriesInstanceUid;
            DicomModality = string.IsNullOrWhiteSpace(dicomModality) ? throw new ArgumentException("dicomModality should be non-empty", nameof(dicomModality)) : dicomModality;

            _hashCode = $"{PatientId}-{StudyInstanceUid}-{SeriesInstanceUid}-{DicomModality}".GetHashCode();
        }

        /// <summary>
        /// Gets the patient identifier.
        /// </summary>
        /// <value>
        /// The patient identifier.
        /// </value>
        public string PatientId { get; }

        /// <summary>
        /// Gets the study instance uid.
        /// </summary>
        /// <value>
        /// The study instance uid.
        /// </value>
        public string StudyInstanceUid { get; }

        /// <summary>
        /// Gets the series instance uid.
        /// </summary>
        /// <value>
        /// The series instance uid.
        /// </value>
        public string SeriesInstanceUid { get; }

        /// <summary>
        /// Gets the DICOM modality.
        /// </summary>
        /// <value>
        /// The DICOM modality.
        /// </value>
        public string DicomModality { get; }

        /// <summary>
        /// Equalses the specified DICOM file information.
        /// </summary>
        /// <param name="dicomFileInformation">The DICOM file information.</param>
        /// <returns>If equal.</returns>
        public bool Equals(DicomFileInformation dicomFileInformation)
        {
            if (dicomFileInformation is null)
            {
                return false;
            }

            return
                PatientId == dicomFileInformation.PatientId &&
                SeriesInstanceUid == dicomFileInformation.SeriesInstanceUid &&
                StudyInstanceUid == dicomFileInformation.StudyInstanceUid &&
                DicomModality == dicomFileInformation.DicomModality;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="dicomFileInformation1">The DICOM file information 1.</param>
        /// <param name="dicomFileInformation2">The DICOM file information 2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(DicomFileInformation dicomFileInformation1, DicomFileInformation dicomFileInformation2)
        {
            if (dicomFileInformation1 is null)
            {
                return dicomFileInformation2 is null;
            }

            return dicomFileInformation1.Equals(dicomFileInformation2);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="dicomFileInformation1">The DICOM file information 1.</param>
        /// <param name="dicomFileInformation2">The DICOM file information 2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(DicomFileInformation dicomFileInformation1, DicomFileInformation dicomFileInformation2)
        {
            if (dicomFileInformation1 is null)
            {
                return !(dicomFileInformation2 is null);
            }

            return !dicomFileInformation1.Equals(dicomFileInformation2);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is DicomFileInformation dicomFileInformation)
            {
                return Equals(dicomFileInformation);
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}