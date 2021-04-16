namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.RegularExpressions;
    using Dicom;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Simple regular expresession based constraint on a DicomTag that can be
    /// converted to string.
    /// </summary>
    public class RegexConstraint : DicomTagConstraint, IEquatable<RegexConstraint>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="index">Dicom tag index.</param>
        /// <param name="expression">Regex expression.</param>
        /// <param name="options">Regex options.</param>
        /// <param name="ordinal">Ordinal.</param>
        [JsonConstructor]
        public RegexConstraint(DicomTagIndex index, string expression, RegexOptions options = RegexOptions.None, int ordinal = 0)
            : base(index)
        {
            Expression = expression;
            Options = options;
            Ordinal = ordinal;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="regex"></param>
        /// <param name="options"></param>
        /// <param name="ordinal"></param>
        public RegexConstraint(DicomTag tag, string regex, RegexOptions options = RegexOptions.None, int ordinal = 0)
            : this(new DicomTagIndex(tag), regex, options, ordinal)
        {
        }

        /// <summary>
        /// The regular expression you wish to check
        /// </summary>
        [Required]
        public string Expression { get; }

        /// <summary>
        /// Options for the regex
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [Required]
        public RegexOptions Options { get; }

        /// <summary>
        /// The ordinal you wish to constrain
        /// </summary>
        [Required]
        public int Ordinal { get; }

        /// <summary>
        /// Test the regular expression with the given options against a string extracted from the dicom tag specified.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <exception cref="ArgumentNullException">If dataset is null </exception>
        /// <returns></returns>
        public override DicomConstraintResult Check(DicomDataset dataSet)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException(nameof(dataSet));
            }

            var r = new Regex(Expression, Options);
            var s = dataSet.GetValue<string>(Index.DicomTag, Ordinal);

            return new DicomConstraintResult(r.IsMatch(s), this);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as RegexConstraint);
        }

        /// <inheritdoc/>
        public bool Equals(RegexConstraint other)
        {
            return other != null &&
                   EqualityComparer<DicomTagIndex>.Default.Equals(Index, other.Index) &&
                   Expression == other.Expression &&
                   Options == other.Options &&
                   Ordinal == other.Ordinal;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1223269981;
            hashCode = hashCode * -1521134295 + EqualityComparer<DicomTagIndex>.Default.GetHashCode(Index);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Expression);
            hashCode = hashCode * -1521134295 + Options.GetHashCode();
            hashCode = hashCode * -1521134295 + Ordinal.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(RegexConstraint left, RegexConstraint right)
        {
            return EqualityComparer<RegexConstraint>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(RegexConstraint left, RegexConstraint right)
        {
            return !(left == right);
        }
    }
}
