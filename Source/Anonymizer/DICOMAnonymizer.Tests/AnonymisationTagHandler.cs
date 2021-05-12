namespace DICOMAnonymizer.Tests
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Dicom;
    using DICOMAnonymizer;
    using AnonFunc = System.Func<Dicom.DicomDataset, System.Collections.Generic.List<TagOrIndex>, Dicom.DicomItem, Dicom.DicomItem>;

    internal class AnonymisationTagHandler : ITagHandler
    {

        /// <summary>
        /// The anonymisation protocol.
        /// </summary>
        private readonly Dictionary<DicomTag, AnonFunc> _anonymisationProtocol = new Dictionary<DicomTag, AnonFunc>
        {
            { DicomTag.PatientID, (ds,tagOrIndexes, dicomItem)=> dicomItem },
            { DicomTag.Modality, (ds,tagOrIndexes, dicomItem)=> dicomItem },
            { DicomTag.SOPClassUID, (ds,tagOrIndexes, dicomItem)=> new DicomUniqueIdentifier(DicomTag.SOPClassUID,DicomUIDGenerator.GenerateDerivedFromUUID()) },
            { DicomTag.SOPInstanceUID, (ds,tagOrIndexes, dicomItem)=> new DicomUniqueIdentifier(DicomTag.SOPInstanceUID,DicomUIDGenerator.GenerateDerivedFromUUID()) },
        };

        // TODO refactor into abstract class
        public Dictionary<string, string> GetConfiguration() => null;

        // TODO refactor into abstract class
        public Dictionary<Regex, AnonFunc> GetRegexFuncs() => null;

        // TODO refactor into abstract class
        public Dictionary<DicomTag, AnonFunc> GetTagFuncs() => _anonymisationProtocol;

        // TODO refactor into abstract class
        public void NextDataset()
        {
        }

        public void Postprocess(DicomDataset newds)
        {
            // NOOP
        }
    }
}