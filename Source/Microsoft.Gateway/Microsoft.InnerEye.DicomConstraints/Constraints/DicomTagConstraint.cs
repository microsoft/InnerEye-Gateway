// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Dicom;

    /// <summary>
    /// The Dicom tag constraint.
    /// </summary>
    /// <seealso cref="DicomConstraint" />
    public class DicomTagConstraint : DicomConstraint
    {
        /// <summary>
        /// Construct a new instance
        /// </summary>
        /// <param name="index"></param>
        protected DicomTagConstraint(DicomTagIndex index)
        {
            Index = index;
        }

        /// <summary>
        /// The tag you wish to constrain
        /// </summary>
        [Required]
        public DicomTagIndex Index { get; }

        /// <inheritdoc />
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            throw new NotImplementedException();
        }
    }
}
