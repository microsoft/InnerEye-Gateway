// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dicom;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Describes how to group sequential constraints
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>
        /// And operator
        /// </summary>
        And,

        /// <summary>
        /// Or operator
        /// </summary>
        Or,
    }

    /// <summary>
    /// A group of constraints that must either all pass or at least 1 pass for this constraint to pass.
    /// </summary>
    public class GroupConstraint : DicomConstraint, IEquatable<GroupConstraint>
    {
        /// <summary>
        /// Construct a new constrain given a list of constraints and a operator to combine them
        /// </summary>
        /// <param name="constraints">List of constraints.</param>
        /// <param name="op">How to combine constraints.</param>
        public GroupConstraint(IReadOnlyList<DicomConstraint> constraints, LogicalOperator op)
        {
            Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
            Op = op;
        }

        /// <summary>
        /// The ordered list of constraints to apply to the dataset
        /// </summary>
        public IReadOnlyList<DicomConstraint> Constraints { get; }

        /// <summary>
        /// The operator for combining Constraints
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public LogicalOperator Op { get; }

        /// <summary>
        /// Check the dataset passes this group constraint
        /// </summary>
        /// <param name="dataSet"></param>
        /// <exception cref="ArgumentNullException">If dataset is null</exception>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException(nameof(dataSet));
            }

            var childResults = Constraints.Select(c => c.Check(dataSet)).ToArray();
            var result = Op == LogicalOperator.And ? childResults.All(c => c.Result) : childResults.Any(c => c.Result);

            return new DicomConstraintResult(result, this, childResults);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as GroupConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(GroupConstraint other)
        {
            return other != null &&
                   Constraints.SequenceEqual(other.Constraints) &&
                   Op == other.Op;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 985604525;
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyList<DicomConstraint>>.Default.GetHashCode(Constraints);
            hashCode = hashCode * -1521134295 + Op.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(GroupConstraint left, GroupConstraint right)
        {
            return EqualityComparer<GroupConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(GroupConstraint left, GroupConstraint right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Impose a group constraint on an individual tag.
    /// </summary>
    public class GroupTagConstraint : DicomTagConstraint, IEquatable<GroupTagConstraint>
    {
        /// <summary>
        /// Impose a group constraint on an indvidual tag.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="index"></param>
        [JsonConstructor]
        public GroupTagConstraint(GroupConstraint group, DicomTagIndex index)
        : base(index)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
        }

        /// <summary>
        /// Impose a group constraint on an indvidual tag.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="tag"></param>
        public GroupTagConstraint(GroupConstraint group, DicomTag tag)
        : this(group, new DicomTagIndex(tag))
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
        }

        /// <summary>
        /// The Group constraint on Index
        /// </summary>
        public GroupConstraint Group { get; }

        /// <summary>
        /// Ask the Group to check the constraint
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            return Group.Check(dataSet);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as GroupTagConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(GroupTagConstraint other)
        {
            return other != null &&
                   EqualityComparer<DicomTagIndex>.Default.Equals(Index, other.Index) &&
                   EqualityComparer<GroupConstraint>.Default.Equals(Group, other.Group);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -2043322065;
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomTagIndex>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<GroupConstraint>.Default.GetHashCode(Group);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(GroupTagConstraint left, GroupTagConstraint right)
        {
            return EqualityComparer<GroupTagConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(GroupTagConstraint left, GroupTagConstraint right)
        {
            return !(left == right);
        }
    }
}
