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
    /// Insist a string contains a certain value
    /// </summary>
    public class StringContainsConstraint : DicomTagConstraint, IEquatable<StringContainsConstraint>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="match"></param>
        /// <param name="ordinal"></param>
        [JsonConstructor]
        public StringContainsConstraint(DicomTagIndex index, string match, int ordinal = 0)
            : base(index)
        {
            Match = match ?? throw new ArgumentNullException(nameof(match));
            Ordinal = ordinal;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public StringContainsConstraint(DicomTag tag, string match, int ordinal = 0)
            : this(new DicomTagIndex(tag), match, ordinal)
        {
        }

        /// <summary>
        /// The string that must be present in the tag
        /// </summary>
        [Required]
        public string Match { get; }

        /// <summary>
        /// The Ordinal you wish to constrain.
        /// </summary>
        [Required]
        public int Ordinal { get; }

        /// <summary>
        /// Checks the Dicom tag contains the given string
        /// </summary>
        /// <exception cref="ArgumentNullException"> If dataset is null</exception>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException(nameof(dataSet));
            }

            var v = dataSet.GetValue<string>(Index.DicomTag, Ordinal);

            return new DicomConstraintResult(v.Contains(Match), this);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as StringContainsConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(StringContainsConstraint other)
        {
            return other != null &&
                   EqualityComparer<DicomTagIndex>.Default.Equals(Index, other.Index) &&
                   Match == other.Match &&
                   Ordinal == other.Ordinal;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -405970557;
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomTagIndex>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Match);
            hashCode = hashCode * -1521134295 + Ordinal.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(StringContainsConstraint left, StringContainsConstraint right)
        {
            return EqualityComparer<StringContainsConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(StringContainsConstraint left, StringContainsConstraint right)
        {
            return !(left == right);
        }
    }
}
