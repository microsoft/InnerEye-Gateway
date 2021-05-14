// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.DicomConstraints.Tests
{
    using Dicom;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GroupTests
    {
        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintsLogicalCombo()
        {
            var c0 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedIntConstraint(Order.Equal, 42, DicomTag.SeriesNumber));
            var c1 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedDoubleConstraint(Order.GreaterThan, 3.141, DicomTag.PixelSpacing, 0));
            var c2 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedDoubleConstraint(Order.LessThan, 6.0, DicomTag.PixelSpacing, 1));
            var c3t = new StringContainsConstraint(DicomTag.PatientPosition, "HFS");
            var c3 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, c3t);

            var groupConstraintAnd = new GroupConstraint(new DicomConstraint[] { c0, c1, c2, c3 }, LogicalOperator.And);
            var groupConstraintOr = new GroupConstraint(new DicomConstraint[] { c0, c1, c2, c3 }, LogicalOperator.Or);

            var ds = new DicomDataset
            {
                { DicomTag.SeriesNumber, 42 },
                { DicomTag.PixelSpacing, new decimal[] { 3.142M, 3.142M } },
                { DicomTag.PatientPosition, "HFS" },
            };

            Assert.IsTrue(groupConstraintAnd.Check(ds).Result);
            Assert.IsTrue(groupConstraintOr.Check(ds).Result);

            ds.AddOrUpdate(DicomTag.PatientPosition, "FAIL");
            Assert.IsFalse(groupConstraintAnd.Check(ds).Result);
            Assert.IsTrue(groupConstraintOr.Check(ds).Result);
        }
    }
}