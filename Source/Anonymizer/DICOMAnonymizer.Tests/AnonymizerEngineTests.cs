namespace DICOMAnonymizer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Dicom;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using static DICOMAnonymizer.AnonymizeEngine;
    using AnonFunc = System.Func<Dicom.DicomDataset, System.Collections.Generic.List<DICOMAnonymizer.TagOrIndex>, Dicom.DicomItem, Dicom.DicomItem>;

    [TestClass]
    public class AnonymizerEngineTests
    {
        private static AnonymizeEngine GetAnonEngine(Mode m)
        {
            return (AnonymizeEngine)Activator.CreateInstance(typeof(AnonymizeEngine), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { m, true }, null, null);
        }

        private class TestHandler : ITagHandler
        {
            public int State { get; set; }

            public TestHandler()
            {
                tagh = new Dictionary<DicomTag, AnonFunc>();
                regh = new Dictionary<Regex, AnonFunc>();
            }

            public TestHandler(Dictionary<DicomTag, AnonFunc> tagh, Dictionary<Regex, AnonFunc> regh)
            {
                this.tagh = tagh;
                this.regh = regh;
            }

            public Dictionary<DicomTag, AnonFunc> tagh = new Dictionary<DicomTag, AnonFunc>();
            public Dictionary<Regex, AnonFunc> regh = new Dictionary<Regex, AnonFunc>();

            public Dictionary<Regex, AnonFunc> GetRegexFuncs() { return regh; }

            public Dictionary<DicomTag, AnonFunc> GetTagFuncs() { return tagh; }

            public void Postprocess(DicomDataset newds) { }

            public void NextDataset() { State++; }

            public Dictionary<string, string> GetConfiguration() { return new Dictionary<string, string> { { "c", "a" } }; }
        }

        [TestMethod]
        public void IgnoreNullReturnsInActions()
        {
            var anon = new AnonymizeEngine(Mode.inplace);
            var th = new TestHandler(null, null);

            anon.RegisterHandler(th);
            anon.ForceRegisterHandler(th);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "A double registration of an action to a tag was prevented.")]
        public void RegisterHandlerShouldNotAllowOverwriting()
        {
            var anon = GetAnonEngine(Mode.inplace);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, (x, s, y) => { return y; } } }, null);

            anon.RegisterHandler(th);
            anon.RegisterHandler(th);
        }

        [TestMethod]
        public void ForceRegisterHandlerShouldAllowOverwriting()
        {
            var anon = GetAnonEngine(Mode.clone);
            var th = new TestHandler();

            // Execute the empty function
            anon.RegisterHandler(th);

            var ds = new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));
            anon.Anonymize(ds);

            th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, (x, s, y) => { throw new ArgumentException("Hello"); } } }, null);
            anon.ForceRegisterHandler(th);

            // Execute the throwing function
            try
            {
                anon.Anonymize(ds);
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("Hello", e.Message);
            }
        }

        [TestMethod]
        public void NestedSequencesInPlace()
        {
            var ds = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomSequence(DicomTag.RTROIObservationsSequence,
                    new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"))));

            var anon = GetAnonEngine(Mode.inplace);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, (x, s, y) => { return new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "3.2.1"); } } }, null);
            anon.RegisterHandler(th);

            anon.Anonymize(ds);

            Assert.AreEqual("3.2.1", ds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.AreEqual(1, ds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("3.2.1", ds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));
        }

        [TestMethod]
        public void NestedSequencesBlankUndeclaredTagsShouldBeRemoved()
        {
            var ds = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomSequence(DicomTag.RTROIObservationsSequence,
                    new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"))));

            var anon = GetAnonEngine(Mode.blank);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, (x, s, y) => { return new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "3.2.1"); } } }, null);
            anon.RegisterHandler(th);

            var nds = anon.Anonymize(ds);

            // Assert no changes happened to the original object
            Assert.AreEqual("1.2.3", ds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsTrue(ds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(1, ds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("1.2.3", ds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));

            Assert.AreEqual("3.2.1", nds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsFalse(nds.Contains(DicomTag.RTROIObservationsSequence));
        }

        [TestMethod]
        public void NestedSequencesBlankDeclaredTagsShouldBeKept()
        {
            var ds = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomSequence(DicomTag.RTROIObservationsSequence,
                    new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"))));

            var anon = GetAnonEngine(Mode.blank);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() {
                { DicomTag.RTROIObservationsSequence,  (x, s, y) => { return y; } },
                { DicomTag.SOPInstanceUID,  (x, s, y) => { return new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "3.2.1"); } }
            }, null);
            anon.RegisterHandler(th);

            var nds = anon.Anonymize(ds);

            // Assert no changes happened to the original object
            Assert.AreEqual("1.2.3", ds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsTrue(ds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(1, ds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("1.2.3", ds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));

            Assert.AreEqual("3.2.1", nds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.AreEqual(1, nds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("3.2.1", nds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));
        }

        [TestMethod]
        public void NestedSequencesCloneUndeclaredTagsShouldBeKept()
        {
            var ds = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomSequence(DicomTag.RTROIObservationsSequence,
                    new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"))));

            var anon = GetAnonEngine(Mode.clone);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, (x, s, y) => { return new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "3.2.1"); } } }, null);
            anon.RegisterHandler(th);

            var nds = anon.Anonymize(ds);

            // Assert no changes happened to the original object
            Assert.AreEqual("1.2.3", ds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsTrue(ds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(1, ds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("1.2.3", ds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));

            Assert.AreEqual("3.2.1", nds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsTrue(nds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(1, nds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("3.2.1", nds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));
        }

        [TestMethod]
        public void NestedSequencesCloneOneDeclaredTagShouldBeRemoved()
        {
            var ds = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomSequence(DicomTag.RTROIObservationsSequence,
                    new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"))));

            var anon = GetAnonEngine(Mode.clone);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() {
                { DicomTag.RTROIObservationsSequence,  (x, s, y) => { return null; } },
                { DicomTag.SOPInstanceUID,  (x, s, y) => { return new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "3.2.1"); } }
            }, null);
            anon.RegisterHandler(th);

            var nds = anon.Anonymize(ds);

            // Assert no changes happened to the original object
            Assert.AreEqual("1.2.3", ds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsTrue(ds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(1, ds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("1.2.3", ds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));

            Assert.AreEqual("3.2.1", nds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsFalse(nds.Contains(DicomTag.RTROIObservationsSequence));
        }

        [TestMethod]
        public void NestedSequencesCloneRemovalAllExceptSequence()
        {
            var ds = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomSequence(DicomTag.RTROIObservationsSequence,
                    new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"))));

            var anon = GetAnonEngine(Mode.clone);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, (x, s, y) => { return null; } } }, null);
            anon.RegisterHandler(th);

            var nds = anon.Anonymize(ds);

            // Assert no changes happened to the original object
            Assert.AreEqual("1.2.3", ds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsTrue(ds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(1, ds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("1.2.3", ds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));

            Assert.IsFalse(nds.Contains(DicomTag.SOPInstanceUID));
            Assert.IsTrue(nds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(0, nds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
        }

        [TestMethod]
        public void NestedSequencesBlankRemovalAllExceptSequence()
        {
            var ds = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomSequence(DicomTag.RTROIObservationsSequence,
                    new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"))));

            var anon = GetAnonEngine(Mode.blank);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.RTROIObservationsSequence, (x, s, y) => { return y; } } }, null);
            anon.RegisterHandler(th);

            var nds = anon.Anonymize(ds);

            // Assert no changes happened to the original object
            Assert.AreEqual("1.2.3", ds.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.IsTrue(ds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(1, ds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
            Assert.AreEqual("1.2.3", ds.GetSequence(DicomTag.RTROIObservationsSequence).Items[0].GetSingleValue<string>(DicomTag.SOPInstanceUID));

            Assert.IsFalse(nds.Contains(DicomTag.SOPInstanceUID));
            Assert.IsTrue(nds.Contains(DicomTag.RTROIObservationsSequence));
            Assert.AreEqual(0, nds.GetSequence(DicomTag.RTROIObservationsSequence).Items.Count);
        }

        [TestMethod]
        public void HandlerOverwriteReporting()
        {
            var anon = GetAnonEngine(Mode.clone);
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() {
                    { DicomTag.RTROIObservationsSequence,  (x, s, y) => { return y; } },
                    { DicomTag.SOPInstanceUID,  (x, s, y) => { return y; } }
                },
                new Dictionary<Regex, AnonFunc>() {
                    { new Regex(".*"), (x, s, y) => { return y; }},
                    { new Regex(".*2"), (x, s, y) => { return y; } },
                });
            anon.RegisterHandler(th);
            var report = anon.ForceRegisterHandler(th);

            Assert.AreEqual(4, report.Count);
            var exp = DicomTag.RTROIObservationsSequence.DictionaryEntry.Name + " " + DicomTag.RTROIObservationsSequence.ToString().ToUpper(CultureInfo.InvariantCulture);
            Assert.AreEqual(exp, report[0]);
            Assert.AreEqual(".*", report[2]);
        }

        [TestMethod]
        public void RegistreredHandlersReport()
        {
            var anon = new AnonymizeEngine(Mode.clone);
            var cp = new ConfidentialityProfile();
            anon.RegisterHandler(cp);

            var report = anon.ReportRegisteredHandlers();

            Assert.AreEqual(336, report.Count);
        }

        [TestMethod]
        public void TestDicomFile()
        {
            var file = DicomFile.Open(@"TestData/CT1_J2KI");

            var anonymizer = GetAnonEngine(Mode.clone);
            var cp = new ConfidentialityProfile();
            anonymizer.RegisterHandler(cp);

            var prev = file.FileMetaInfo.MediaStorageSOPInstanceUID;
            Assert.AreNotEqual("", prev.ToString());

            var nf = anonymizer.Anonymize(file);

            Assert.AreEqual(0, nf.Dataset.GetValues<string>(DicomTag.PatientName).Length);
            Assert.AreEqual(0, nf.Dataset.GetValues<string>(DicomTag.PatientID).Length);
            Assert.AreEqual(0, nf.Dataset.GetValues<string>(DicomTag.PatientSex).Length);
            Assert.AreNotEqual(prev.ToString(), nf.FileMetaInfo.MediaStorageSOPInstanceUID.ToString());
            Assert.IsNull(nf.FileMetaInfo.ImplementationVersionName);
            Assert.IsNull(nf.FileMetaInfo.SourceApplicationEntityTitle);
            Assert.AreEqual(file.FileMetaInfo.TransferSyntax, nf.FileMetaInfo.TransferSyntax);
        }

        [TestMethod]
        public void TestDicomFileBlank()
        {
            var file = DicomFile.Open(@"TestData/CT1_J2KI");

            var anonymizer = GetAnonEngine(Mode.blank);
            var cp = new AnonymisationTagHandler();
            anonymizer.RegisterHandler(cp);

            var prev = file.FileMetaInfo.MediaStorageSOPInstanceUID;
            Assert.AreNotEqual("", prev.ToString());

            var nf = anonymizer.Anonymize(file);
            Assert.AreEqual(file.Dataset.GetSingleValue<string>(DicomTag.PatientID), nf.Dataset.GetSingleValue<string>(DicomTag.PatientID));
            Assert.AreEqual(file.Dataset.GetSingleValue<string>(DicomTag.Modality), nf.Dataset.GetSingleValue<string>(DicomTag.Modality));
            Assert.AreNotEqual(file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID), nf.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.AreNotEqual(file.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID), nf.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID));
            Assert.AreNotEqual(prev.ToString(), nf.FileMetaInfo.MediaStorageSOPInstanceUID.ToString());
            Assert.IsNull(nf.FileMetaInfo.ImplementationVersionName);
            Assert.IsNull(nf.FileMetaInfo.SourceApplicationEntityTitle);
            Assert.AreEqual(file.FileMetaInfo.TransferSyntax, nf.FileMetaInfo.TransferSyntax);
        }

        [TestMethod]
        public void TestDicomMissingTransferSyntax()
        {
            var file = DicomFile.Open(@"TestData/CT1_J2KI");
            file.FileMetaInfo.Remove(DicomTag.TransferSyntaxUID);
            var anonymizer = GetAnonEngine(Mode.blank);
            var cp = new AnonymisationTagHandler();
            anonymizer.RegisterHandler(cp);
            var prev = file.FileMetaInfo.MediaStorageSOPInstanceUID;
            Assert.AreNotEqual("", prev.ToString());

            var nf = anonymizer.Anonymize(file);

            Assert.AreEqual(file.Dataset.GetSingleValue<string>(DicomTag.PatientID), nf.Dataset.GetSingleValue<string>(DicomTag.PatientID));
            Assert.AreEqual(file.Dataset.GetSingleValue<string>(DicomTag.Modality), nf.Dataset.GetSingleValue<string>(DicomTag.Modality));
            Assert.AreNotEqual(file.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID), nf.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.AreNotEqual(file.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID), nf.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID));
            Assert.AreNotEqual(prev.ToString(), nf.FileMetaInfo.MediaStorageSOPInstanceUID.ToString());
            Assert.IsNull(nf.FileMetaInfo.ImplementationVersionName);
            Assert.IsNull(nf.FileMetaInfo.SourceApplicationEntityTitle);
            Assert.IsFalse(file.FileMetaInfo.Contains(DicomTag.TransferSyntaxUID));
            Assert.AreEqual(DicomTransferSyntax.ExplicitVRLittleEndian, nf.FileMetaInfo.TransferSyntax);
        }

        [TestMethod]
        public void TestNextDataset()
        {
            var ds = new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));
            var anon = GetAnonEngine(Mode.clone);
            AnonFunc f = (o, s, i) => { return null; };
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, f } }, null);
            anon.RegisterHandler(th);

            anon.Anonymize(ds);
            Assert.AreEqual(1, th.State);

            anon.Anonymize(ds);
            Assert.AreEqual(2, th.State);
        }

        [TestMethod]
        public void TestTagPath()
        {
            var ds = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomSequence(DicomTag.RTROIObservationsSequence,
                    new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                                     new DicomSequence(DicomTag.ReferenceBasisCodeSequence,
                                        new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"))))));

            var anon = GetAnonEngine(Mode.inplace);

            var d = 0;
            AnonFunc f = (o, s, i) =>
            {
                if (d == 0)
                {
                    Assert.AreEqual(0, s.Count);
                    d++;
                }
                else if (d == 1)
                {
                    Assert.AreEqual(2, s.Count);
                    Assert.AreEqual(DicomTag.RTROIObservationsSequence, s[0].Tag);
                    Assert.IsTrue(s[0].IsTag);
                    Assert.AreEqual(0, s[1].Index);
                    Assert.IsFalse(s[1].IsTag);
                    d++;
                }
                else if (d == 2)
                {
                    Assert.AreEqual(4, s.Count);
                    Assert.AreEqual(DicomTag.RTROIObservationsSequence, s[0].Tag);
                    Assert.AreEqual(DicomTag.ReferenceBasisCodeSequence, s[2].Tag);
                }
                return i;
            };
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, f } }, null);
            anon.RegisterHandler(th);

            anon.Anonymize(ds);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestDescriptionIsRequired()
        {
            var anon = new AnonymizeEngine(Mode.clone);
            AnonFunc f = (o, s, i) => { return null; };
            var th = new TestHandler(new Dictionary<DicomTag, AnonFunc>() { { DicomTag.SOPInstanceUID, f } }, null);
            anon.RegisterHandler(th);
        }
    }
}
