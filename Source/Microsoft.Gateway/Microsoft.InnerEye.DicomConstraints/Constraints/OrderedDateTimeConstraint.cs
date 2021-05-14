// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    public class OrderedDateTimeConstraint : DicomTagConstraint, IEquatable<OrderedDateTimeConstraint>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="function">Ordering function parameters</param>
        [JsonConstructor]
        public OrderedDateTimeConstraint(DicomTagIndex index, DicomOrderedTag<DateTime> function)
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
        public OrderedDateTimeConstraint(Order order, DateTime value, DicomTag tag, int ordinal = 0)
            : this(new DicomTagIndex(tag), new DicomOrderedTag<DateTime>(order, value, ordinal))
        {
        }

        /// <summary>
        /// Ordering function parameters
        /// </summary>
        [Required]
        public DicomOrderedTag<DateTime> Function { get; }

        /// <summary>
        /// Checks that the tag in the given dataset satiisfies the ordering function
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            return BaseOrderConstraint<DateTime, DateTime, DefaultSelector<DateTime>>.
                Check(dataSet, Function.Order, Function.Value, Index.DicomTag, this, Function.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as OrderedDateTimeConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(OrderedDateTimeConstraint other)
        {
            return other != null &&
                   EqualityComparer<DicomTagIndex>.Default.Equals(Index, other.Index) &&
                   EqualityComparer<DicomOrderedTag<DateTime>>.Default.Equals(Function, other.Function);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 426273644;
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomTagIndex>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomOrderedTag<DateTime>>.Default.GetHashCode(Function);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(OrderedDateTimeConstraint left, OrderedDateTimeConstraint right)
        {
            return EqualityComparer<OrderedDateTimeConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(OrderedDateTimeConstraint left, OrderedDateTimeConstraint right)
        {
            return !(left == right);
        }
    }
}
