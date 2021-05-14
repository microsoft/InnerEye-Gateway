// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json;

    /// <summary>
    /// The string comparison function to use to impose an
    /// </summary>
    public enum StringComparisonType
    {
        /// <summary>
        /// CulureInvariantCaseSensitive
        /// </summary>
        CulureInvariantCaseSensitive = 0,

        /// <summary>
        /// CultureInvariantIgnoreCase
        /// </summary>
        CultureInvariantIgnoreCase,
    }

    /// <summary>
    /// A simple string wrapper that can be used for string comparisons with the BaseOrderConstraint class.
    /// </summary>
    public class OrderedString : IComparable, IInitializable<string>, IEquatable<OrderedString>
    {
        /// <summary>
        /// Construct a new ordering of the given Value using the StringComparisonType specified
        /// </summary>
        /// <param name="value"></param>
        /// <param name="comparisonType"></param>
        [JsonConstructor]
        public OrderedString(string value, StringComparisonType comparisonType = StringComparisonType.CulureInvariantCaseSensitive)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            ComparisonType = comparisonType;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public OrderedString()
        {
            Value = string.Empty;
            ComparisonType = StringComparisonType.CulureInvariantCaseSensitive;
        }

        /// <summary>
        /// The value you wish to compare to
        /// </summary>
        [Required]
        public string Value { get; private set; }

        /// <summary>
        /// How you would like to compare with Value
        /// </summary>
        [Required]
        public StringComparisonType ComparisonType { get; }

        /// <summary>
        /// implicitly convert value to an OrderString with CulureInvariantCaseSensitive comparison
        /// </summary>
        /// <param name="value">Value to convert.</param>
        public static implicit operator OrderedString(string value)
        {
            return FromString(value);
        }

        /// <summary>
        /// Convert from a string to an OrderString with CulureInvariantCaseSensitive comparison.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <returns>New <see cref="OrderedString"/>.</returns>
        public static OrderedString FromString(string value) => new OrderedString(value);

        /// <summary>
        /// IComparable implementation
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj is OrderedString)
            {
                var objOrdered = obj as OrderedString;
                switch (ComparisonType)
                {
                    default:
                    case StringComparisonType.CulureInvariantCaseSensitive:
                        {
                            return StringComparer.InvariantCulture.Compare(Value, objOrdered.Value);
                        }

                    case StringComparisonType.CultureInvariantIgnoreCase:
                        {
                            return StringComparer.InvariantCultureIgnoreCase.Compare(Value, objOrdered.Value);
                        }
                }
            }
            else
            {
                throw new ArgumentException("Parameter not an OrderedString", nameof(obj));
            }
        }

        /// <summary>
        /// Implementation of IInitializable
        /// </summary>
        /// <param name="source"></param>
        public void Init(string source)
        {
            Value = source;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as OrderedString);
        }

        /// <inheritdoc/>
        public bool Equals(OrderedString other)
        {
            return other != null &&
                   Value == other.Value &&
                   ComparisonType == other.ComparisonType;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1031581766;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + ComparisonType.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(OrderedString left, OrderedString right)
        {
            return EqualityComparer<OrderedString>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(OrderedString left, OrderedString right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator <(OrderedString left, OrderedString right)
        {
            return left is null ? !(right is null) : left.CompareTo(right) < 0;
        }

        /// <inheritdoc/>
        public static bool operator <=(OrderedString left, OrderedString right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        /// <inheritdoc/>
        public static bool operator >(OrderedString left, OrderedString right)
        {
            return !(left is null) && left.CompareTo(right) > 0;
        }

        /// <inheritdoc/>
        public static bool operator >=(OrderedString left, OrderedString right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
    }
}
