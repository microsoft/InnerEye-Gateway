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
    /// Impose a constraint on a DicomUID
    /// </summary>
    public class UIDStringOrderConstraint : DicomTagConstraint, IEquatable<UIDStringOrderConstraint>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        [JsonConstructor]
        public UIDStringOrderConstraint(DicomTagIndex index, DicomOrderedTag<OrderedString> function)
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
        public UIDStringOrderConstraint(Order order, OrderedString value, DicomTag tag, int ordinal = 0)
            : this(new DicomTagIndex(tag), new DicomOrderedTag<OrderedString>(order, value, ordinal))
        {
        }

        /// <summary>
        /// Ordering function parameters
        /// </summary>
        [Required]
        public DicomOrderedTag<OrderedString> Function { get; }

        /// <summary>
        /// Runs the ordering check on the given dataset
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            return BaseOrderConstraint<OrderedString, DicomUID, OrderedUIDStringSelector>.
                Check(dataSet, Function.Order, Function.Value, Index.DicomTag, this, Function.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as UIDStringOrderConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(UIDStringOrderConstraint other)
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
        public static bool operator ==(UIDStringOrderConstraint left, UIDStringOrderConstraint right)
        {
            return EqualityComparer<UIDStringOrderConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(UIDStringOrderConstraint left, UIDStringOrderConstraint right)
        {
            return !(left == right);
        }
    }
}
