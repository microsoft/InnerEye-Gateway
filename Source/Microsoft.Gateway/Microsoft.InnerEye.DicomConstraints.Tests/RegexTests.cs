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
        [TestCategory("RegexConstraintTests")]
        [DataRow("^ORIG", 0)]
        [DataRow("ARY$", 1)]
        [DataRow("X.A", 2)]
        [TestMethod]
        public void RegexSomeOrdinal(string regex, int ordinal)
        {
            regex = regex ?? throw new ArgumentNullException(nameof(regex));

            var dataset = new DicomDataset
            {
                { DicomTag.ImageType, new  [] {"ORIGINAL","PRIMARY", "AXIAL" } },
            };

            for (var i = 0; i < 3; i++)
            {
                var constraint = new RegexConstraint(DicomTag.ImageType, regex, RegexOptions.None, i);
                Assert.AreEqual(i == ordinal, constraint.Check(dataset).Result);
            }
        }

        [TestCategory("RegexConstraintTests")]
        [DataRow("^ORIG", true)]
        [DataRow("ARY$", true)]
        [DataRow("X.A", true)]
        [DataRow("^ORIH", false)]
        [DataRow("ARZ$", false)]
        [DataRow(@"X\dA", false)]
        [TestMethod]
        public void RegexAnyOrdinal(string regex, bool expected)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.ImageType, new  [] {"ORIGINAL","PRIMARY", "AXIAL" } },
            };

            var constraint = new RegexConstraint(DicomTag.ImageType, regex, RegexOptions.None, -1);
            Assert.AreEqual(expected, constraint.Check(dataset).Result);
        }
    }
}
