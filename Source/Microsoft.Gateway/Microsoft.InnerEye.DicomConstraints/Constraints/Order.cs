// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints
{
    /// <summary>
    /// Encodes an operator on a pair of comparable items.
    /// </summary>
    public enum Order : int
    {
        /// <summary>
        /// For all A &amp; B :  A Never B = false
        /// </summary>
        Never = 0,

        /// <summary>
        /// For all A &amp; B : A LessThan B => A &lt; B
        /// </summary>
        LessThan = 1,

        /// <summary>
        /// For all A &amp; B : A Equal B => A = B
        /// </summary>
        Equal = 2,

        /// <summary>
        /// For all A &amp; B : A LessThanOrEqual B => (A LessThan B || A Equals B)
        /// </summary>
        LessThanOrEqual = 3,

        /// <summary>
        /// For all A &amp; B : A GreaterThan B => A > B
        /// </summary>
        GreaterThan = 4,

        /// <summary>
        /// For all A &amp; B : A NotEqual B => A LessThan B || A GreaterThan B
        /// </summary>
        NotEqual = 5,

        /// <summary>
        /// For all A &amp; B : A GreaterThanOrEqual B => A Equal B | A GreaterThan B
        /// </summary>
        GreaterThanOrEqual = 6,

        /// <summary>
        /// For all A &amp; B : A Always B => true (A &lt; LessThan B | A Equal B | A GreaterThan B)
        /// </summary>
        Always = 7,
    }
}
