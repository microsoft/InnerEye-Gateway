namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System.ComponentModel.DataAnnotations;

    using Dicom;

    using Microsoft.InnerEye.DicomConstraints;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Encodes how a DICOM tag is to be treated within a protocol.
    /// </summary>
    public class DicomTagAnonymisation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DicomTagAnonymisation"/> class.
        /// </summary>
        /// <param name="dicomTag">The dicom tag.</param>
        /// <param name="anonymisationProtocol">The anonymisation protocol.</param>
        public DicomTagAnonymisation(DicomTag dicomTag, AnonymisationMethod anonymisationProtocol)
            : this(new DicomTagIndex(dicomTag), anonymisationProtocol)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomTagAnonymisation"/> class.
        /// </summary>
        /// <param name="dicomTagIndex">The serializable Dicom tag.</param>
        /// <param name="anonymisationProtocol">The anonymisation protocol.</param>
        [JsonConstructor]
        public DicomTagAnonymisation(DicomTagIndex dicomTagIndex, AnonymisationMethod anonymisationProtocol)
        {
            DicomTagIndex = dicomTagIndex;
            AnonymisationProtocol = anonymisationProtocol;
        }

        /// <summary>
        /// Gets the serializable dicom tag index.
        /// </summary>
        /// <value>
        /// The serializable dicom tag index.
        /// </value>
        [Required]
        public DicomTagIndex DicomTagIndex { get; }

        /// <summary>
        /// Gets the anonymisation protocol that should be used for this DICOM tag.
        /// </summary>
        /// <value>
        /// The anonymisation protocol for this DICOM tag.
        /// </value>
        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public AnonymisationMethod AnonymisationProtocol { get; }
    }
}