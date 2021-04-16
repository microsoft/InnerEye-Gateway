namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Encodes an ordering function on a DicomTag
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DicomOrderedTag<T> : IEquatable<DicomOrderedTag<T>>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="order"></param>
        /// <param name="value"></param>
        /// <param name="ordinal"></param>
        [JsonConstructor]
        public DicomOrderedTag(Order order, T value, int ordinal = 0)
        {
            Order = order;
            Value = value;
            Ordinal = ordinal;
        }

        /// <summary>
        /// The ordering function to apply between this Value and the tag value
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [Required]
        public Order Order { get; }

        /// <summary>
        /// The value to use in the constraint function
        /// </summary>
        [Required]
        public T Value { get; }

        /// <summary>
        /// The Ordinal we wish to extract from the tag or -1 to extract all
        /// </summary>
        [Required]
        [Range(-1, int.MaxValue)]
        public int Ordinal { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as DicomOrderedTag<T>);
        }

        /// <inheritdoc/>
        public bool Equals(DicomOrderedTag<T> other)
        {
            return other != null &&
                   Order == other.Order &&
                   EqualityComparer<T>.Default.Equals(Value, other.Value) &&
                   Ordinal == other.Ordinal;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1130906755;
            hashCode = hashCode * -1521134295 + Order.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + Ordinal.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(DicomOrderedTag<T> left, DicomOrderedTag<T> right)
        {
            return EqualityComparer<DicomOrderedTag<T>>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(DicomOrderedTag<T> left, DicomOrderedTag<T> right)
        {
            return !(left == right);
        }
    }
}
