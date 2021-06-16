// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints.Tests
{
    using System;
    using System.Text.RegularExpressions;
    using Dicom;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RegexTests
    {
        /// <summary>
        /// RegexConstraint tests.
        /// </summary>
        /// <param name="regex">Regex to test.</param>
        /// <param name="ordinal">Ordinal to test at.</param>
        /// <param name="expected">Expected value of constraint.</param>
        [TestCategory("RegexConstraintTests")]
        [DataRow("^ORIG", 0, true)]
        [DataRow("ARY$", 1, true)]
        [DataRow("X.A", 2, true)]
        [DataRow("^ORIH", 0, false)]
        [DataRow("ARZ$", 1, false)]
        [DataRow(@"X\dA", 2, false)]
        [TestMethod]
        public void RegexConstraintVMTests(string regex, int ordinal, bool expected)
        {
            regex = regex ?? throw new ArgumentNullException(nameof(regex));

            var dataset = new DicomDataset
            {
                { DicomTag.ImageType, new[] { "ORIGINAL", "PRIMARY", "AXIAL" } },
            };

            for (var i = -1; i < 3; i++)
            {
                var constraint = new RegexConstraint(DicomTag.ImageType, regex, RegexOptions.None, i);

                // If ordinal == -1 then this should always match, otherwise only for given ordinal.
                var ordinalMatch = i == -1 || i == ordinal;

                Assert.AreEqual(ordinalMatch && expected, constraint.Check(dataset).Result);
            }
        }
    }
}
