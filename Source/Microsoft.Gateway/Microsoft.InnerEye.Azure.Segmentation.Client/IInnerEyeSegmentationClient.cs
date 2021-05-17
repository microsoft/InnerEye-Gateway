// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Azure.Segmentation.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Dicom;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;

    /// <summary>
    /// Client SDK to talk to InnerEye Cloud Segmentation Service
    /// </summary>
    public interface IInnerEyeSegmentationClient : IDisposable
    {
        /// <summary>
        /// Gets the hard-coded anonymisation protocol used for sending anonymising file through the segmentation API.
        /// This is not used for Upload as the anonymisation protocol is configurable.
        /// </summary>
        /// <value>
        /// The anonymisation protocol used for the segmentation API.
        /// </value>
        IEnumerable<DicomTagAnonymisation> SegmentationAnonymisationProtocol { get; }

        /// <summary>
        /// Gets the segmentation anonymisation protocol identifier.
        /// </summary>
        /// <value>
        /// The segmentation anonymisation protocol identifier.
        /// </value>
        Guid SegmentationAnonymisationProtocolId { get; }

        /// <summary>
        /// Gets the list of DicomTags that are copied directly from the reference DicomFile to the deanonymised output DicomFile.
        /// </summary>
        IEnumerable<DicomTag> TopLevelReplacements { get; }

        /// <summary>
        /// Checks the client can ping the segmentation service API.
        /// </summary>
        /// <returns>A waitable task.</returns>
        Task PingAsync();

        /// <summary>
        /// Anonymizes the dicom file.
        /// </summary>
        /// <param name="dicomFile">The dicom file.</param>
        /// <param name="anonymisationProtocolId">The anonymisation protocol unqiue identifier.</param>
        /// <param name="anonymisationProtocol">The anonymisation protocol.</param>
        /// <returns>The anonymized DICOM file.</returns>
        DicomFile AnonymizeDicomFile(DicomFile dicomFile, Guid anonymisationProtocolId, IEnumerable<DicomTagAnonymisation> anonymisationProtocol);

        /// <summary>
        /// Anonymizes the input DicomFiles.
        /// </summary>
        /// <param name="dicomFiles">The Dicom files to anonymize.</param>
        /// <param name="anonymisationProtocolId">The anonymisation protocol unqiue identifier.</param>
        /// <param name="anonymisationProtocol">The anonymisation protocol.</param>
        /// <returns>The anonymized DicomFiles.</returns>
        /// <exception cref="ArgumentNullException">If the DicomFiles are null.</exception>
        /// <exception cref="ArgumentException">If any of the DicomFiles in the collection are null.</exception>
        IEnumerable<DicomFile> AnonymizeDicomFiles(IEnumerable<DicomFile> dicomFiles, Guid anonymisationProtocolId, IEnumerable<DicomTagAnonymisation> anonymisationProtocol);

        /// <summary>
        /// Gets the de-anonymised RT file for a segmentation
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="segmentationId">The segmentation identifier.</param>
        /// <param name="referenceDicomFiles">The reference dicom files.</param>
        /// <param name="userReplacements">The user replacements for result rt file.</param>
        /// <returns></returns>
        Task<ModelResult> SegmentationResultAsync(
            string modelId,
            string segmentationId,
            IEnumerable<DicomFile> referenceDicomFiles,
            IEnumerable<TagReplacement> userReplacements);

        /// <summary>
        /// Creates a task to segment dicom files, it anonymizes the input images
        /// </summary>
        /// <param name="modelId">Model to use when doing segmentation.</param>
        /// <param name="channelIdsAndDicomFiles">DICOM dataset encoding the images as byte[] indexed by AutoSegmentationModel.ChannelIds</param>
        /// <returns>The segmentationId and the anonymized images</returns>
        Task<(string segmentationId, IEnumerable<DicomFile> postedImages)> StartSegmentationAsync(
            string modelId,
            IEnumerable<ChannelData> channelIdsAndDicomFiles);

        /// <summary>
        /// Deanonymizes a DicomFile and applies replacements in the following order:
        /// 1 - topLevelReplacements
        /// 2 - hashed Dicom tags
        /// 3 - userReplacements
        /// </summary>
        /// <param name="dicomFile">DicomFile to be deanonymized.</param>
        /// <param name="referenceDicomFiles">Reference DicomFiles with patient data. At least one file required.</param>
        /// <param name="topLevelReplacements">Top level patient and study data replacements.</param>
        /// <param name="userReplacements">User replacements.</param>
        /// <param name="anonymisationProtocolId">Anonymisation protocol unqiue identifier.</param>
        /// <param name="anonymisationProtocol">Anonymisation protocol.</param>
        /// <returns>Deanonymized DicomFile.</returns>
        DicomFile DeanonymizeDicomFile(
            DicomFile dicomFile,
            IEnumerable<DicomFile> referenceDicomFiles,
            IEnumerable<DicomTag> topLevelReplacements,
            IEnumerable<TagReplacement> userReplacements,
            Guid anonymisationProtocolId,
            IEnumerable<DicomTagAnonymisation> anonymisationProtocol);
    }
}
