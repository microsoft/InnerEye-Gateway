namespace Microsoft.InnerEye.DicomConstraints
{
    using Dicom;
    using Newtonsoft.Json;
    using NJsonSchema.Converters;
    using System.Runtime.Serialization;

    /// <summary>
    /// Impose a constraint on a DICOM dataset
    /// </summary>
    [JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
    [KnownType(typeof(OrderedIntConstraint))]
    [KnownType(typeof(OrderedDoubleConstraint))]
    [KnownType(typeof(OrderedDateTimeConstraint))]
    [KnownType(typeof(OrderedStringConstraint))]
    [KnownType(typeof(TimeOrderConstraint))]
    [KnownType(typeof(RegexConstraint))]
    [KnownType(typeof(UIDStringOrderConstraint))]
    [KnownType(typeof(RequiredTagConstraint))]
    [KnownType(typeof(GroupConstraint))]
    [KnownType(typeof(GroupTagConstraint))]
    [KnownType(typeof(StringContainsConstraint))]
    public abstract class DicomConstraint
    {
        /// <summary>
        /// Check that the dataset conforms to this constraint.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public abstract DicomConstraintResult Check(DicomDataset dataSet);
    }
}