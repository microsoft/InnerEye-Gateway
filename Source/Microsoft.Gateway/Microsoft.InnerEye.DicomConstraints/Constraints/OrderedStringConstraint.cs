namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Dicom;
    using Newtonsoft.Json;

    /// <summary>
    /// Serialization type for OrderConstraint char
    /// </summary>
    public class OrderedStringConstraint : DicomTagConstraint, IEquatable<OrderedStringConstraint>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="function">Ordering function parameters</param>
        [JsonConstructor]
        public OrderedStringConstraint(DicomTagIndex index, DicomOrderedTag<OrderedString> function)
            : base(index)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="order"></param>
        /// <param name="value"></param>
        /// <param name="tag"></param>
        /// <param name="ordinal"></param>
        public OrderedStringConstraint(Order order, OrderedString value, DicomTag tag, int ordinal = 0)
            : this(new DicomTagIndex(tag), new DicomOrderedTag<OrderedString>(order, value, ordinal))
        {
        }

        /// <summary>
        /// Ordering function parameters
        /// </summary>
        [Required]
        public DicomOrderedTag<OrderedString> Function { get; }

        /// <summary>
        /// Checks that the tag in the given dataset satiisfies the ordering function
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            return BaseOrderConstraint<OrderedString, string, ConvertSelector<string, OrderedString>>.
                Check(dataSet, Function.Order, Function.Value, Index.DicomTag, this, Function.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as OrderedStringConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(OrderedStringConstraint other)
        {
            return other != null &&
                   EqualityComparer<DicomTagIndex>.Default.Equals(Index, other.Index) &&
                   EqualityComparer<DicomOrderedTag<OrderedString>>.Default.Equals(Function, other.Function);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 426273644;
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomTagIndex>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomOrderedTag<OrderedString>>.Default.GetHashCode(Function);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(OrderedStringConstraint left, OrderedStringConstraint right)
        {
            return EqualityComparer<OrderedStringConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(OrderedStringConstraint left, OrderedStringConstraint right)
        {
            return !(left == right);
        }
    }
}
