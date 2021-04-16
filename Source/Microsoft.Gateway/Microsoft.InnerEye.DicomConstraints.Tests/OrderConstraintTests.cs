namespace Microsoft.InnerEye.DicomConstraints.Tests
{
    using System;
    using System.Text.RegularExpressions;

    using Dicom;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OrderConstraintTests
    {
        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintsTestOrder()
        {
            var o0 = new OrderedIntConstraint(Order.Never, 42, DicomTag.SeriesNumber);
            var o1 = new OrderedIntConstraint(Order.LessThan, 42, DicomTag.SeriesNumber);
            var o2 = new OrderedIntConstraint(Order.LessThanOrEqual, 42, DicomTag.SeriesNumber);
            var o3 = new OrderedIntConstraint(Order.Equal, 42, DicomTag.SeriesNumber);
            var o4 = new OrderedIntConstraint(Order.GreaterThan, 42, DicomTag.SeriesNumber);
            var o5 = new OrderedIntConstraint(Order.GreaterThanOrEqual, 42, DicomTag.SeriesNumber);
            var o6 = new OrderedIntConstraint(Order.NotEqual, 42, DicomTag.SeriesNumber);
            var o7 = new OrderedIntConstraint(Order.Always, 42, DicomTag.SeriesNumber);

            DicomDataset ds = new DicomDataset
            {
                { DicomTag.SeriesNumber, 41 },
            };

            Assert.IsFalse(o0.Check(ds).Result);
            Assert.IsTrue(o1.Check(ds).Result);
            Assert.IsTrue(o2.Check(ds).Result);
            Assert.IsFalse(o3.Check(ds).Result);
            Assert.IsFalse(o4.Check(ds).Result);
            Assert.IsFalse(o5.Check(ds).Result);
            Assert.IsTrue(o6.Check(ds).Result);
            Assert.IsTrue(o7.Check(ds).Result);

            ds.AddOrUpdate(DicomTag.SeriesNumber, 42);
            Assert.IsFalse(o0.Check(ds).Result);
            Assert.IsFalse(o1.Check(ds).Result);
            Assert.IsTrue(o2.Check(ds).Result);
            Assert.IsTrue(o3.Check(ds).Result);
            Assert.IsFalse(o4.Check(ds).Result);
            Assert.IsTrue(o5.Check(ds).Result);
            Assert.IsFalse(o6.Check(ds).Result);
            Assert.IsTrue(o7.Check(ds).Result);

            ds.AddOrUpdate(DicomTag.SeriesNumber, 43);
            Assert.IsFalse(o0.Check(ds).Result);
            Assert.IsFalse(o1.Check(ds).Result);
            Assert.IsFalse(o2.Check(ds).Result);
            Assert.IsFalse(o3.Check(ds).Result);
            Assert.IsTrue(o4.Check(ds).Result);
            Assert.IsTrue(o5.Check(ds).Result);
            Assert.IsTrue(o6.Check(ds).Result);
            Assert.IsTrue(o7.Check(ds).Result);
        }

        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintsTime()
        {
            var o0 = new TimeOrderConstraint(Order.Never, new TimeSpan(11, 11, 11), DicomTag.SeriesTime);
            var o1 = new TimeOrderConstraint(Order.LessThan, new TimeSpan(11, 11, 11), DicomTag.SeriesTime);
            var o2 = new TimeOrderConstraint(Order.LessThanOrEqual, new TimeSpan(11, 11, 11), DicomTag.SeriesTime);
            var o3 = new TimeOrderConstraint(Order.Equal, new TimeSpan(11, 11, 11), DicomTag.SeriesTime);
            var o4 = new TimeOrderConstraint(Order.GreaterThan, new TimeSpan(11, 11, 11), DicomTag.SeriesTime);
            var o5 = new TimeOrderConstraint(Order.GreaterThanOrEqual, new TimeSpan(11, 11, 11), DicomTag.SeriesTime);
            var o6 = new TimeOrderConstraint(Order.NotEqual, new TimeSpan(11, 11, 11), DicomTag.SeriesTime);
            var o7 = new TimeOrderConstraint(Order.Always, new TimeSpan(11, 11, 11), DicomTag.SeriesTime);

            DicomDataset ds = new DicomDataset
            {
                { DicomTag.SeriesTime, new DateTime(2017, 2, 14, 10, 11, 11) },
            };

            Assert.IsFalse(o0.Check(ds).Result);
            Assert.IsTrue(o1.Check(ds).Result);
            Assert.IsTrue(o2.Check(ds).Result);
            Assert.IsFalse(o3.Check(ds).Result);
            Assert.IsFalse(o4.Check(ds).Result);
            Assert.IsFalse(o5.Check(ds).Result);
            Assert.IsTrue(o6.Check(ds).Result);
            Assert.IsTrue(o7.Check(ds).Result);

            ds.AddOrUpdate(DicomTag.SeriesTime, new DateTime(2017, 2, 14, 11, 11, 11));
            Assert.IsFalse(o0.Check(ds).Result);
            Assert.IsFalse(o1.Check(ds).Result);
            Assert.IsTrue(o2.Check(ds).Result);
            Assert.IsTrue(o3.Check(ds).Result);
            Assert.IsFalse(o4.Check(ds).Result);
            Assert.IsTrue(o5.Check(ds).Result);
            Assert.IsFalse(o6.Check(ds).Result);
            Assert.IsTrue(o7.Check(ds).Result);

            ds.AddOrUpdate(DicomTag.SeriesTime, new DateTime(2017, 2, 14, 13, 11, 11));
            Assert.IsFalse(o0.Check(ds).Result);
            Assert.IsFalse(o1.Check(ds).Result);
            Assert.IsFalse(o2.Check(ds).Result);
            Assert.IsFalse(o3.Check(ds).Result);
            Assert.IsTrue(o4.Check(ds).Result);
            Assert.IsTrue(o5.Check(ds).Result);
            Assert.IsTrue(o6.Check(ds).Result);
            Assert.IsTrue(o7.Check(ds).Result);
        }

        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintRegex()
        {
            var c0 = new RegexConstraint(DicomTag.SeriesDescription, @"(hog)", RegexOptions.ECMAScript);
            var c0R = new RequiredTagConstraint(TagRequirement.Optional, c0);

            DicomDataset ds = new DicomDataset();
            ds.AddOrUpdate(DicomTag.SeriesDescription, @"hog protocol v1");

            Assert.IsTrue(c0R.Check(ds).Result);
        }

        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintUID()
        {
            DicomDataset ds = new DicomDataset();
            ds.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.CTImageStorage);

            var s0 = new UIDStringOrderConstraint(Order.Equal, DicomUID.CTImageStorage.UID, DicomTag.SOPClassUID);
            var c0 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, s0);
            Assert.IsTrue(c0.Check(ds).Result);
        }

        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintCodeString()
        {
            DicomDataset ds = new DicomDataset();
            ds.AddOrUpdate(DicomTag.BodyPartExamined, "PELVIS");

            var d0 = new OrderedStringConstraint(Order.NotEqual, "SKULL", DicomTag.BodyPartExamined);
            var c0 = new RequiredTagConstraint(TagRequirement.Optional, d0);
            Assert.IsTrue(c0.Check(ds).Result);
        }

        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintCodeStringCases()
        {
            DicomDataset ds = new DicomDataset();
            ds.AddOrUpdate(DicomTag.BodyPartExamined, "PELVIS");

            var orderedCaseSensitive = new OrderedString("pElViS", StringComparisonType.CultureInvariantIgnoreCase);
            var orderedCaseInsensitive = new OrderedString("pElViS", StringComparisonType.CulureInvariantCaseSensitive);
            var d0 = new OrderedStringConstraint(Order.Equal, orderedCaseSensitive, DicomTag.BodyPartExamined);
            Assert.IsTrue(d0.Check(ds).Result);
            var d1 = new OrderedStringConstraint(Order.NotEqual, orderedCaseInsensitive, DicomTag.BodyPartExamined);
            Assert.IsTrue(d1.Check(ds).Result);
        }
    }
}
