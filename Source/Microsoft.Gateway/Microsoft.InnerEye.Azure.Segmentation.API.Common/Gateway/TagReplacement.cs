namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.InnerEye.DicomConstraints;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// TagReplacement
    /// </summary>
    public class TagReplacement : IEquatable<TagReplacement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagReplacement"/> class.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <param name="dicomTagIndex">Index of the dicom tag.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">
        /// dicomTagIndex
        /// or
        /// value
        /// </exception>
        public TagReplacement(TagReplacementOperation operation, DicomTagIndex dicomTagIndex, string value)
        {
            Operation = operation;
            DicomTagIndex = dicomTagIndex ?? throw new ArgumentNullException(nameof(dicomTagIndex));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <value>
        /// The operation.
        /// </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public TagReplacementOperation Operation { get; }

        /// <summary>
        /// Gets the index of the dicom tag.
        /// </summary>
        /// <value>
        /// The index of the dicom tag.
        /// </value>
        public DicomTagIndex DicomTagIndex { get; }

        /// <summary>
        /// Gets the value for the operation
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as TagReplacement);
        }

        /// <inheritdoc/>
        public bool Equals(TagReplacement other)
        {
            return other != null &&
                   Operation == other.Operation &&
                   EqualityComparer<DicomTagIndex>.Default.Equals(DicomTagIndex, other.DicomTagIndex) &&
                   Value == other.Value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1049562115;
            hashCode = hashCode * -1521134295 + Operation.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomTagIndex>.Default.GetHashCode(DicomTagIndex);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(TagReplacement left, TagReplacement right)
        {
            return EqualityComparer<TagReplacement>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(TagReplacement left, TagReplacement right)
        {
            return !(left == right);
        }
    }
}
