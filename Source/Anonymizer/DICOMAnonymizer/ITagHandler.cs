namespace DICOMAnonymizer
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Dicom;

    /// TODO: Ideally, argument DicomDataset oldds should be declared as immutable.
    using AnonFunc = System.Func<Dicom.DicomDataset, System.Collections.Generic.List<TagOrIndex>, Dicom.DicomItem, Dicom.DicomItem>;

    public interface ITagHandler
    {
        // TODO: 
        // 1) Ideally we want to expose functions with exactly the correct types per tag. 
        // e.g. a tag that is a UID should be Func<DicomDataset, DicomUniqueIdentifier, DicomUniqueIdentifier>
        // or even: Func<DicomDataset, string, string> as its VR is string
        // 2) Return values for Sequences should be bool. Or else users can edit the returned tag.
        // 3) Maybe return a custom class that wraps operation (add,del,mod) with an object.
        Dictionary<DicomTag, AnonFunc> GetTagFuncs();
        Dictionary<Regex, AnonFunc> GetRegexFuncs();

        /// <summary>
        /// Gives access to the processed dataset after the anonymization process. Should be
        /// used for adding custom tags.
        /// </summary>
        /// <param name="newds"></param>
        void Postprocess(DicomDataset newds);

        /// <summary>
        /// Get the current configuration of a TagHandler. The anonymization engine can then
        /// report the configuration options declared for each anonymization step.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetConfiguration();

        /// <summary>
        /// Inform the TagHandlers that a new Dataset will be processed. Use
        /// to clean internal state.
        /// </summary>
        void NextDataset();
    }
}
