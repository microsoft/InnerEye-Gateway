// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints.Tests
{
    using System;
    using System.Globalization;
    using Dicom;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StringContainsTests
    {
        /// <summary>
        /// StringContainsConstraint tests.
        /// </summary>
        /// <param name="match">Substring to test.</param>
        /// <param name="ordinal">Ordinal to test at.</param>
        /// <param name="expected">Expected value of constraint.</param>
        [TestCategory("StringContainsConstraints")]
        [DataRow("RIG", 0, true)]
        [DataRow("RIM", 1, true)]
        [DataRow("XIA", 2, true)]
        [DataRow("RIH", 0, false)]
        [DataRow("RIN", 1, false)]
        [DataRow("XIB", 2, false)]
        [TestMethod]
        public void StringContainsConstraintVMTest(string match, int ordinal, bool expected)
        {
            match = match ?? throw new ArgumentNullException(nameof(match));

            var dataset = new DicomDataset
            {
                { DicomTag.ImageType, new[] { "ORIGINAL", "PRIMARY", "AXIAL" } },
            };

            for (var i = -1; i < 3; i++)
            {
                var constraintSome = new StringContainsConstraint(DicomTag.ImageType, match, i);

                // If ordinal == -1 then this should always match, otherwise only for given ordinal.
                var ordinalMatch = i == -1 || i == ordinal;

                Assert.AreEqual(ordinalMatch && expected, constraintSome.Check(dataset).Result);

                // Check for case sensitivity, this should always fail.
                var constraintLower = new StringContainsConstraint(DicomTag.ImageType, match.ToLower(CultureInfo.CurrentCulture), i);
                Assert.IsFalse(constraintLower.Check(dataset).Result);
            }
        }
    }
}
