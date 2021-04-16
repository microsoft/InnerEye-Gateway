using Dicom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DICOMAnonymizer.AnonymizeEngine;
using static DICOMAnonymizer.ConfidentialityProfile;

namespace DICOMAnonymizer.Tests
{
    [TestClass]
    public class ConfidentialityProfileTests
    {
        [TestMethod]
        public void CheckEmptyStringComparison()
        {
            var engine = new AnonymizeEngine(Mode.clone);

            var profileOpts = SecurityProfileOptions.BasicProfile
                | SecurityProfileOptions.RetainLongModifDates
                | SecurityProfileOptions.CleanDesc;

            var cp = new ConfidentialityProfile(profileOpts);

            engine.RegisterHandler(cp);
        }

        [TestMethod]
        public void AnonymizeInPlace_Dataset_PatientDataEmpty()
        {
            var dataset = DicomFile.Open(@"TestData/CT1_J2KI").Dataset;

            var anonymizer = new AnonymizeEngine(Mode.inplace);
            var cp = new ConfidentialityProfile();
            anonymizer.RegisterHandler(cp);

            anonymizer.Anonymize(dataset);

            Assert.AreEqual(0, dataset.GetValues<string>(DicomTag.PatientName).Length);
            Assert.AreEqual(0, dataset.GetValues<string>(DicomTag.PatientID).Length);
            Assert.AreEqual(0, dataset.GetValues<string>(DicomTag.PatientSex).Length);
        }

        [TestMethod]
        public void AnonymizeInPlace_File_SopInstanceUidTransferredToMetaInfo()
        {
            var file = DicomFile.Open(@"TestData/CT1_J2KI");
            var old = file.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPInstanceUID);

            var anonymizer = new AnonymizeEngine(Mode.inplace);
            var cp = new ConfidentialityProfile();
            anonymizer.RegisterHandler(cp);

            file = anonymizer.Anonymize(file);

            var expected = file.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPInstanceUID);
            var actual = file.FileMetaInfo.MediaStorageSOPInstanceUID;
            Assert.AreNotEqual(expected, old);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void AnonymizeInPlace_File_ImplementationVersionNameNotMaintained()
        {
            var file = DicomFile.Open(@"TestData/CT1_J2KI");
            var expected = file.FileMetaInfo.ImplementationVersionName;

            Assert.IsFalse(string.IsNullOrEmpty(expected));

            var anonymizer = new AnonymizeEngine(Mode.inplace);
            var cp = new ConfidentialityProfile();
            anonymizer.RegisterHandler(cp);

            file = anonymizer.Anonymize(file);

            var actual = file.FileMetaInfo.ImplementationVersionName;
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void Anonymize_Dataset_OriginalDatasetNotModified()
        {
            var dataset = DicomFile.Open(@"TestData/CT-MONO2-16-ankle").Dataset;
            var expected = dataset.GetSingleValue<DicomUID>(DicomTag.StudyInstanceUID);

            var anonymizer = new AnonymizeEngine(Mode.clone);
            var cp = new ConfidentialityProfile();
            anonymizer.RegisterHandler(cp);

            var newDataset = anonymizer.Anonymize(dataset);

            var actual = dataset.GetSingleValue<DicomUID>(DicomTag.StudyInstanceUID);
            var actualNew = newDataset.GetSingleValue<DicomUID>(DicomTag.StudyInstanceUID);

            Assert.AreEqual(expected, actual);
            Assert.AreNotEqual(expected, actualNew);
        }

        [TestMethod]
        public void CheckRemovals()
        {
            var ds = new DicomDataset(new DicomShortString(DicomTag.PerformedProcedureStepID, "123"));

            var cp = new ConfidentialityProfile();

            var anonymizer = new AnonymizeEngine(Mode.blank);
            anonymizer.RegisterHandler(cp);
            var nds_blank = anonymizer.Anonymize(ds);
            Assert.IsFalse(nds_blank.Contains(DicomTag.PerformedProcedureStepID));
            Assert.AreEqual("123", ds.GetSingleValue<string>(DicomTag.PerformedProcedureStepID));

            anonymizer = new AnonymizeEngine(Mode.clone);
            anonymizer.RegisterHandler(cp);
            var nds_clone = anonymizer.Anonymize(ds);
            Assert.IsFalse(nds_clone.Contains(DicomTag.PerformedProcedureStepID));
            Assert.AreEqual("123", ds.GetSingleValue<string>(DicomTag.PerformedProcedureStepID));

            anonymizer = new AnonymizeEngine(Mode.inplace);
            anonymizer.RegisterHandler(cp);
            anonymizer.Anonymize(ds);
            Assert.IsFalse(ds.Contains(DicomTag.PerformedProcedureStepID));

        }

        [TestMethod]
        public void CheckThereIsNoGlobalCachedState()
        {
            var anonymizer = new AnonymizeEngine(Mode.inplace);
            var cp = new ConfidentialityProfile(SecurityProfileOptions.BasicProfile);
            anonymizer.RegisterHandler(cp);

            var ds = new DicomDataset(new DicomUniqueIdentifier(DicomTag.RequestedSOPInstanceUID, "123"));

            anonymizer.Anonymize(ds);
            Assert.AreNotEqual("123", ds.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID));

            cp = new ConfidentialityProfile(SecurityProfileOptions.BasicProfile | SecurityProfileOptions.RetainUIDs);
            anonymizer.ForceRegisterHandler(cp);

            ds = new DicomDataset(new DicomUniqueIdentifier(DicomTag.RequestedSOPInstanceUID, "123"));

            anonymizer.Anonymize(ds);
            Assert.AreEqual("123", ds.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID));
        }

        [TestMethod]
        public void CheckWhitelistTags()
        {
            var cp = new ConfidentialityProfile(SecurityProfileOptions.BasicProfile | SecurityProfileOptions.RetainUIDs);

            var ds = new DicomDataset(new DicomUniqueIdentifier(DicomTag.RequestedSOPInstanceUID, "123"));

            var anonymizer = new AnonymizeEngine(Mode.blank);
            anonymizer.RegisterHandler(cp);
            var nds_blank = anonymizer.Anonymize(ds);
            Assert.AreEqual("123", ds.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID));
            Assert.AreEqual("123", nds_blank.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID));

            anonymizer = new AnonymizeEngine(Mode.clone);
            anonymizer.RegisterHandler(cp);
            var nds_clone = anonymizer.Anonymize(ds);
            Assert.AreEqual("123", ds.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID));
            Assert.AreEqual("123", nds_clone.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID));

            anonymizer = new AnonymizeEngine(Mode.inplace);
            anonymizer.RegisterHandler(cp);
            anonymizer.Anonymize(ds);
            Assert.AreEqual("123", ds.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID));
        }
    }
}
