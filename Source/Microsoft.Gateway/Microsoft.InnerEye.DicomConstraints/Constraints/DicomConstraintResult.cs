// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dicom;

    /// <summary>
    /// Dicom constraint result.
    /// </summary>
    public class DicomConstraintResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DicomConstraintResult"/> class.
        /// </summary>
        /// <param name="result">if set to <c>true</c> [result].</param>
        /// <param name="constraint">The constraint.</param>
        public DicomConstraintResult(bool result, DicomConstraint constraint)
        {
            Result = result;
            Constraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomConstraintResult"/> class.
        /// </summary>
        /// <param name="result">if set to <c>true</c> [result].</param>
        /// <param name="constraint">The constraint.</param>
        /// <param name="childResults">The child results.</param>
        public DicomConstraintResult(bool result, DicomConstraint constraint, params DicomConstraintResult[] childResults)
            : this(result, constraint)
        {
            ChildResults = childResults;
        }

        /// <summary>
        /// Test the predicate against the tag in the DICOM dataset and return a new <see cref="DicomConstraintResult"/> class.
        /// </summary>
        /// <param name="dataSet">DICOM dataset to test.</param>
        /// <param name="tag">Tag in dataset to test.</param>
        /// <param name="ordinal">Ordinal in tag to test.</param>
        /// <param name="predicate">Predicate to use for test.</param>
        /// <param name="constraint">Source constraint.</param>
        /// <returns>New DicomConstraintResult.</returns>
        public static DicomConstraintResult Check<T>(DicomDataset dataSet, DicomTag tag, int ordinal, Func<T, bool> predicate, DicomConstraint constraint)
        {
            dataSet = dataSet ?? throw new ArgumentNullException(nameof(dataSet));
            predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

            if (ordinal >= 0)
            {
                var s = dataSet.GetValue<T>(tag, ordinal);

                return new DicomConstraintResult(predicate(s), constraint);
            }
            else
            {
                var vs = dataSet.GetValues<T>(tag);

                return new DicomConstraintResult(vs.Any(predicate), constraint);
            }
        }

        /// <summary>
        /// Gets a the check result of the Dicom constraint.
        /// </summary>
        /// <value>
        ///   <c>true</c> if result; otherwise, <c>false</c>.
        /// </value>
        public bool Result { get; }

        /// <summary>
        /// Gets the constraint.
        /// </summary>
        /// <value>
        /// The constraint.
        /// </value>
        public DicomConstraint Constraint { get; }

        /// <summary>
        /// Gets the child results.
        /// </summary>
        /// <value>
        /// The child results.
        /// </value>
        public IReadOnlyList<DicomConstraintResult> ChildResults { get; }
    }
}