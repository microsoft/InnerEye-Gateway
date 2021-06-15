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
        [TestCategory("StringContainsConstraints")]
        [DataRow("RIG", 0)]
        [DataRow("RIM", 1)]
        [DataRow("XIA", 2)]
        [TestMethod]
        public void StringContainsSomeOrdinal(string match, int ordinal)
        {
            match = match ?? throw new ArgumentNullException(nameof(match));

            var dataset = new DicomDataset
            {
                { DicomTag.ImageType, new  [] {"ORIGINAL","PRIMARY", "AXIAL" } },
            };

            for (var i = 0; i < 3; i++)
            {
                var constraint = new StringContainsConstraint(DicomTag.ImageType, match, i);
                Assert.AreEqual(i == ordinal, constraint.Check(dataset).Result);

                var constraintLower = new StringContainsConstraint(DicomTag.ImageType, match.ToLower(CultureInfo.CurrentCulture), i);
                Assert.IsFalse(constraintLower.Check(dataset).Result);
            }
        }

        [TestCategory("StringContainsConstraints")]
        [DataRow("RIG", true)]
        [DataRow("RIM", true)]
        [DataRow("XIA", true)]
        [DataRow("RIH", false)]
        [DataRow("RIN", false)]
        [DataRow("XIB", false)]
        [TestMethod]
        public void StringContainsAnyOrdinal(string match, bool expected)
        {
            var dataset = new DicomDataset
            {
                { DicomTag.ImageType, new  [] {"ORIGINAL","PRIMARY", "AXIAL" } },
            };

            var constraint = new StringContainsConstraint(DicomTag.ImageType, match, -1);
            Assert.AreEqual(expected, constraint.Check(dataset).Result);
        }
    }
}
