// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints.Tests
{
    using Dicom;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StringContainsTests
    {
        [TestCategory("StringContainsConstraints")]
        [TestMethod]
        public void StringContainsSomeOrdinal()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.ImageType, new  [] {"ORIGINAL","PRIMARY", "AXIAL" } },
            };

            for (var i = 0; i < 2; i++)
            {
                // The first two components don't contain "XIA"
                var constraint = new StringContainsConstraint(DicomTag.ImageType, "XIA", i);
                Assert.IsFalse(constraint.Check(dataset).Result);
            }

            // The last component does contain "XIA"
            var constraint2 = new StringContainsConstraint(DicomTag.ImageType, "XIA", 2);
            Assert.IsTrue(constraint2.Check(dataset).Result);

            // Check that this is case sensitive
            var constraint3 = new StringContainsConstraint(DicomTag.ImageType, "xia", 2);
            Assert.IsFalse(constraint3.Check(dataset).Result);
        }

        [TestCategory("StringContainsConstraints")]
        [TestMethod]
        public void StringContainsAnyOrdinal()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.ImageType, new  [] {"ORIGINAL","PRIMARY", "AXIAL" } },
            };

            // The first component does contain "RIG"
            var constraint1 = new StringContainsConstraint(DicomTag.ImageType, "RIG", -1);
            Assert.IsTrue(constraint1.Check(dataset).Result);

            // The second component does contain "RIM"
            var constraint2 = new StringContainsConstraint(DicomTag.ImageType, "RIM", -1);
            Assert.IsTrue(constraint2.Check(dataset).Result);

            // The last component does contain "XIA"
            var constraint3 = new StringContainsConstraint(DicomTag.ImageType, "XIA", -1);
            Assert.IsTrue(constraint3.Check(dataset).Result);
        }

    }
}
