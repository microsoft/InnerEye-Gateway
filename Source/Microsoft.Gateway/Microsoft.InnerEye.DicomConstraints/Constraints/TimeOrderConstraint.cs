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
    /// An IDicomConstraint for tags of TM value representation
    /// </summary>
    public class TimeOrderConstraint : DicomTagConstraint, IEquatable<TimeOrderConstraint>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        [JsonConstructor]
        public TimeOrderConstraint(DicomTagIndex index, DicomOrderedTag<TimeSpan> function)
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
        public TimeOrderConstraint(Order order, TimeSpan value, DicomTag tag, int ordinal = 0)
            : this(new DicomTagIndex(tag), new DicomOrderedTag<TimeSpan>(order, value, ordinal))
        {
        }

        /// <summary>
        /// Ordering function parameters.
        /// </summary>
        [Required]
        public DicomOrderedTag<TimeSpan> Function { get; }

        /// <summary>
        /// Checks that the tag in the given dataset satiisfies the ordering function
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet) =>
            BaseOrderConstraint.Check<TimeSpan, DateTime, TimeSelector>(dataSet, Function, this);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as TimeOrderConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(TimeOrderConstraint other)
        {
            return other != null &&
                   EqualityComparer<DicomTagIndex>.Default.Equals(Index, other.Index) &&
                   EqualityComparer<DicomOrderedTag<TimeSpan>>.Default.Equals(Function, other.Function);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 426273644;
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomTagIndex>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomOrderedTag<TimeSpan>>.Default.GetHashCode(Function);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(TimeOrderConstraint left, TimeOrderConstraint right)
        {
            return EqualityComparer<TimeOrderConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(TimeOrderConstraint left, TimeOrderConstraint right)
        {
            return !(left == right);
        }
    }
}
