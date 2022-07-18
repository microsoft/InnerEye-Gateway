// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        private readonly DicomTag _dicomTagID;
        private readonly AnonymisationMethod _anonymisationProtocol;
        /// <summary>
        /// Initializes a new instance of the <see cref="DicomTagAnonymisation"/> class.
        /// </summary>
        /// <param name="dicomTagID">The dicom tag name.</param>
        /// <param name="anonymisationProtocol">The anonymisation protocol.</param>
        public DicomTagAnonymisation(DicomTag dicomTagID, AnonymisationMethod anonymisationProtocol)
        {
            _dicomTagID = dicomTagID;
            _anonymisationProtocol = anonymisationProtocol;
        }

        /// <summary>
        /// Gets the name of the DICOM tag.
        /// </summary
        [Required]
        public DicomTag DicomTagID => _dicomTagID;

        /// <summary>
        /// Gets the serializable dicom tag index.
        /// </summary>
        /// <value>
        /// The serializable dicom tag index.
        /// </value>
        [JsonIgnore]
        public DicomTagIndex DicomTagIndex => new DicomTagIndex(_dicomTagID);

        /// <summary>
        /// Gets the anonymisation protocol that should be used for this DICOM tag.
        /// </summary>
        /// <value>
        /// The anonymisation protocol for this DICOM tag.
        /// </value>
        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public AnonymisationMethod AnonymisationProtocol => _anonymisationProtocol;
    }
}