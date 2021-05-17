// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;

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