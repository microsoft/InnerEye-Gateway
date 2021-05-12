namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Dicom;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Encodes the requirement on a tag existing in a dataset
    /// </summary>
#pragma warning disable CA1008 // Enums should have zero value
    public enum TagRequirement : int
#pragma warning restore CA1008 // Enums should have zero value
    {
        /// <summary>
        /// The tag must be present and the conditions on the tag must pass
        /// This would typically be a Type 1 Tag
        /// </summary>
        PresentNotEmpty = 1,

        /// <summary>
        /// The tag must be present and the conditions must pass when the tag is non-empty
        /// This would typically be a Type 2 Tag
        /// </summary>
        PresentCanBeEmpty = 2,

        /// <summary>
        /// The tag does not need to be present but the condition must pass if the tag is present and non-empty
        /// This would typically be a Type 3 Tag
        /// </summary>
        Optional = 3,
    }

    /// <summary>
    /// RequiredTagConstraint
    /// </summary>
    public class RequiredTagConstraint : DicomConstraint, IEquatable<RequiredTagConstraint>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="requirementLevel"></param>
        /// <param name="constraint"></param>
        [JsonConstructor]
        public RequiredTagConstraint(TagRequirement requirementLevel, DicomTagConstraint constraint)
        {
            RequirementLevel = requirementLevel;
            Constraint = constraint;
        }

        /// <summary>
        /// Requirement for the presence of this tag
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [Required]
        public TagRequirement RequirementLevel { get; }

        /// <summary>
        /// The constraint to apply on the tag value
        /// </summary>
        [Required]
        public DicomTagConstraint Constraint { get; }

        /// <summary>
        /// Inspect the given dataset for the specified tag {group:index} if the tag is required to be there
        /// (RequirementLevel) apply Constraint
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

            var tag = Constraint.Index.DicomTag;

            if (dataSet.Contains(tag))
            {
                // Line was if (!string.IsNullOrEmpty(dataSet.Get(tag, string.Empty))) before conversion to new OSS fo-dicom
                if (!string.IsNullOrEmpty(dataSet.GetString(tag)))
                {
                    var childResult = Constraint.Check(dataSet);

                    return new DicomConstraintResult(childResult.Result, this, childResult);
                }
                else
                {
                    if (RequirementLevel < TagRequirement.PresentCanBeEmpty)
                    {
                        // Must be there and non-empty
                        return new DicomConstraintResult(false, this);
                    }
                    else
                    {
                        // Can be empty and is empty
                        return new DicomConstraintResult(true, this);
                    }
                }
            }
            else
            {
                if (RequirementLevel < TagRequirement.Optional)
                {
                    // must be there is not
                    return new DicomConstraintResult(false, this);
                }
                else
                {
                    // Optional and !in the dataset.
                    return new DicomConstraintResult(true, this);
                }
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as RequiredTagConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(RequiredTagConstraint other)
        {
            return other != null &&
                   RequirementLevel == other.RequirementLevel &&
                   EqualityComparer<DicomTagConstraint>.Default.Equals(Constraint, other.Constraint);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1394406664;
            hashCode = hashCode * -1521134295 + RequirementLevel.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomTagConstraint>.Default.GetHashCode(Constraint);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(RequiredTagConstraint left, RequiredTagConstraint right)
        {
            return EqualityComparer<RequiredTagConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(RequiredTagConstraint left, RequiredTagConstraint right)
        {
            return !(left == right);
        }
    }
}
