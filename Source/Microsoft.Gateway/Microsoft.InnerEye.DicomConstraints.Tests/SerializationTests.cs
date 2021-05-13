namespace Microsoft.InnerEye.DicomConstraints.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Dicom;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    [TestClass]
    public class SerializationTests
    {
        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintsTestSerialize()
        {
            var c0 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedIntConstraint(Order.Equal, 42, DicomTag.SeriesNumber));
            var c1 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedDoubleConstraint(Order.GreaterThan, 3.141, DicomTag.PixelSpacing, 0));
            var c2 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedDoubleConstraint(Order.LessThan, 6.0, DicomTag.PixelSpacing, 1));
            var c3t = new StringContainsConstraint(DicomTag.PatientPosition, "HFS");
            var c3 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, c3t);

            var groupConstraintAnd = new GroupConstraint(new DicomConstraint[] { c0, c1, c2, c3 }, LogicalOperator.And);
            var groupConstraintOr = new GroupConstraint(new DicomConstraint[] { c0, c1, c2, c3 }, LogicalOperator.Or);

            var ds1 = new DicomDataset
            {
                { DicomTag.SeriesNumber, 42 },
                { DicomTag.PixelSpacing, new decimal[] { 3.142M, 3.142M } },
                { DicomTag.PatientPosition, "HFS" },
            };

            Assert.IsTrue(groupConstraintAnd.Check(ds1).Result);
            Assert.IsTrue(groupConstraintOr.Check(ds1).Result);

            var ds2 = new DicomDataset
            {
                { DicomTag.SeriesNumber, 42 },
                { DicomTag.PixelSpacing, new decimal[] { 0.5M, 3.142M } },
                { DicomTag.PatientPosition, "HFS" },
            };

            Assert.IsFalse(groupConstraintAnd.Check(ds2).Result);
            Assert.IsTrue(groupConstraintOr.Check(ds2).Result);

            var ss1 = JsonConvert.SerializeObject(groupConstraintAnd);
            var ss2 = JsonConvert.SerializeObject(groupConstraintOr);

            var groupConstraintAndDS = JsonConvert.DeserializeObject<GroupConstraint>(ss1);
            var groupConstraintOrDS = JsonConvert.DeserializeObject<GroupConstraint>(ss2);

            Assert.IsTrue(groupConstraintAndDS.Check(ds1).Result);
            Assert.IsTrue(groupConstraintOrDS.Check(ds1).Result);

            Assert.IsFalse(groupConstraintAndDS.Check(ds2).Result);
            Assert.IsTrue(groupConstraintOrDS.Check(ds2).Result);
        }

        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintsNestedSerialize()
        {
            var c0 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedIntConstraint(Order.Equal, 42, DicomTag.SeriesNumber));
            var c1 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedDoubleConstraint(Order.GreaterThan, 3.141f, DicomTag.PixelSpacing, 0));
            var c2 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, new OrderedDoubleConstraint(Order.LessThan, 6.0f, DicomTag.PixelSpacing, 1));
            var c3t = new StringContainsConstraint(DicomTag.PatientPosition, "HFS");
            var c3 = new RequiredTagConstraint(TagRequirement.PresentNotEmpty, c3t);

            var groupConstraintAnd1 = new GroupConstraint(new DicomConstraint[] { c0, c1 }, LogicalOperator.And);
            var groupConstraintAnd2 = new GroupConstraint(new DicomConstraint[] { c2, c3 }, LogicalOperator.And);

            var groupConstraintOr = new GroupConstraint(new DicomConstraint[] { groupConstraintAnd1, groupConstraintAnd2 }, LogicalOperator.Or);

            var ds1 = new DicomDataset
            {
                { DicomTag.SeriesNumber, 42 },
                { DicomTag.PixelSpacing, new decimal[] { 3.142M, 3.142M } },
                { DicomTag.PatientPosition, "HFS" },
            };

            Assert.IsTrue(groupConstraintOr.Check(ds1).Result);

            // Serialize
            var ss2 = JsonConvert.SerializeObject(groupConstraintOr);

            var groupConstraintOrDS = JsonConvert.DeserializeObject<GroupConstraint>(ss2);

            Assert.IsTrue(groupConstraintOrDS.Check(ds1).Result);
        }

        /// <summary>
        /// Used to generate the current set of constraint types
        /// </summary>
        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void ConstraintsGenTypes()
        {
            var orderInt = new OrderedIntConstraint(Order.Equal, int.MinValue, DicomTag.SeriesNumber);
            var orderDouble = new OrderedDoubleConstraint(Order.Equal, double.MaxValue, DicomTag.SeriesNumber);
            var orderDateTime = new OrderedDateTimeConstraint(Order.Equal, DateTime.UtcNow, DicomTag.SeriesNumber);
            var orderString = new OrderedStringConstraint(Order.Equal, new OrderedString("hOOf", StringComparisonType.CulureInvariantCaseSensitive), DicomTag.SeriesNumber);

            var stringOrderUID = new UIDStringOrderConstraint(Order.Equal, DicomUID.CTImageStorage.UID, DicomTag.SOPClassUID);
            var timeOrder = new TimeOrderConstraint(Order.GreaterThanOrEqual, new TimeSpan(0, 0, 0), DicomTag.SeriesTime);
            var stringContains = new StringContainsConstraint(DicomTag.PatientPosition, "HFS");
            var stringRegex = new RegexConstraint(DicomTag.SeriesDescription, "(hog)", RegexOptions.ECMAScript);
            var compareStrings = new[] { "HOOF", "LEAF", "FLOWER", "HOG" }.Select(t => new OrderedString(t));
            var stringCompountGroup = new GroupConstraint(compareStrings.Select(c => new OrderedStringConstraint(Order.Equal, c, DicomTag.BodyPartExamined)).ToArray(), LogicalOperator.Or);
            var stringCompound = new GroupTagConstraint(stringCompountGroup, DicomTag.BodyPartExamined);

            DicomConstraint[] dc = new[]
            {
                new RequiredTagConstraint(TagRequirement.Optional, orderInt),
                new RequiredTagConstraint(TagRequirement.Optional, orderDouble),
                new RequiredTagConstraint(TagRequirement.Optional, orderDateTime),
                new RequiredTagConstraint(TagRequirement.Optional, orderString),
                new RequiredTagConstraint(TagRequirement.Optional, stringOrderUID),
                new RequiredTagConstraint(TagRequirement.PresentCanBeEmpty, timeOrder),
                new RequiredTagConstraint(TagRequirement.PresentCanBeEmpty, stringContains),
                new RequiredTagConstraint(TagRequirement.Optional, stringRegex),
                new RequiredTagConstraint(TagRequirement.PresentNotEmpty, stringOrderUID),
                new RequiredTagConstraint(TagRequirement.Optional, orderString),
                new RequiredTagConstraint(TagRequirement.Optional, stringCompound),
            };

            var dc2 = new GroupConstraint(dc, LogicalOperator.Or) as DicomConstraint;
            JsonConvert.SerializeObject(dc2);
        }

        [TestCategory("DicomConstraints")]
        [TestMethod]
        public void TestCurrentTypes()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Microsoft.InnerEye.DicomConstraints.Tests.currenttypes.json";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var result = reader.ReadToEnd();

                var constraintGroup = JsonConvert.DeserializeObject<GroupConstraint>(result);
                var ds = new DicomDataset();
                constraintGroup.Check(ds);
            }
        }
    }
}
