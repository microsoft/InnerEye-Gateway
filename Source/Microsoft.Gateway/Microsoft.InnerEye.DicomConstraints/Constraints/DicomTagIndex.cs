namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using Dicom;

    using Newtonsoft.Json;

    /// <summary>
    /// Json serializable DicomTag encoding
    /// </summary>
    public class DicomTagIndex : IEquatable<DicomTagIndex>
    {
        /// <summary>
        /// Constructor for DICOM tag and index
        /// </summary>
        [JsonConstructor]
        public DicomTagIndex(ushort group, ushort element)
        {
            Group = group;
            Element = element;
        }

        /// <summary>
        /// Constructor for DICOM tag and index
        /// </summary>
        /// <param name="tag"></param>
        public DicomTagIndex(DicomTag tag)
            : this(tag?.Group ?? 0, tag?.Element ?? 0)
        {
        }

        /// <summary>
        /// The group index of this tag
        /// </summary>
        [Required]
        [Range(0, 65535)]
        public ushort Group { get; }

        /// <summary>
        /// The element index of this tag
        /// </summary>
        [Required]
        [Range(0, 65535)]
        public ushort Element { get; }

        /// <summary>
        /// Gets the dicom tag.
        /// </summary>
        /// <value>
        /// The dicom tag.
        /// </value>
        [JsonIgnore]
        public DicomTag DicomTag => new DicomTag(Group, Element);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as DicomTagIndex);
        }

        /// <inheritdoc/>
        public bool Equals(DicomTagIndex other)
        {
            return other != null &&
                   Group == other.Group &&
                   Element == other.Element;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -1357862800;
            hashCode = hashCode * -1521134295 + Group.GetHashCode();
            hashCode = hashCode * -1521134295 + Element.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(DicomTagIndex left, DicomTagIndex right)
        {
            return EqualityComparer<DicomTagIndex>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(DicomTagIndex left, DicomTagIndex right)
        {
            return !(left == right);
        }
    }
}
