namespace Microsoft.InnerEye.DicomConstraints.Tests
{
    using Dicom;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TagRequirements
    {
        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintsTestOptionality()
        {
            DicomDataset ds = new DicomDataset();

            var c0 = new OrderedIntConstraint(Order.Equal, 4, DicomTag.SeriesNumber);

            var c3 = new RequiredTagConstraint(TagRequirement.Optional, c0);
            var c2 = new RequiredTagConstraint(TagRequirement.PresentCanBeEmpty, c0);
            var c1 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, c0);

            Assert.IsTrue(c3.Check(ds).Result);
            Assert.IsFalse(c2.Check(ds).Result);
            Assert.IsFalse(c1.Check(ds).Result);

            ds.Add(DicomTag.SeriesNumber, string.Empty);
            Assert.IsTrue(c3.Check(ds).Result);
            Assert.IsTrue(c2.Check(ds).Result);
            Assert.IsFalse(c1.Check(ds).Result);

            ds.AddOrUpdate(DicomTag.SeriesNumber, "4");
            Assert.IsTrue(c3.Check(ds).Result);
            Assert.IsTrue(c2.Check(ds).Result);
            Assert.IsTrue(c1.Check(ds).Result);

            ds.AddOrUpdate(DicomTag.SeriesNumber, "3");
            Assert.IsFalse(c3.Check(ds).Result);
            Assert.IsFalse(c2.Check(ds).Result);
            Assert.IsFalse(c1.Check(ds).Result);
        }
    }
}