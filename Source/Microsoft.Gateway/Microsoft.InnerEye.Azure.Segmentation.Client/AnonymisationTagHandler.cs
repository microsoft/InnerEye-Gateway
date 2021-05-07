namespace Microsoft.InnerEye.Azure.Segmentation.Client
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Dicom;

    using DICOMAnonymizer;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;

    using static DICOMAnonymizer.AnonymizeEngine;

    using AnonFunc = System.Func<Dicom.DicomDataset, System.Collections.Generic.List<DICOMAnonymizer.TagOrIndex>, Dicom.DicomItem, Dicom.DicomItem>;

    internal class AnonymisationTagHandler : ITagHandler
    {
        /// <summary>
        /// The tag handler deidentification name format.
        /// </summary>
        private const string TagHandlerDeidentificationNameFormat = "MS InnerEye {0}: {1}";

        /// <summary>
        /// The anonymisation protocol identifier
        /// </summary>
        private readonly Guid _anonymisationProtocolId;

        /// <summary>
        /// The assembly version.
        /// </summary>
        private readonly string _assemblyVersion;

        /// <summary>
        /// The anonymisation protocol.
        /// </summary>
        private readonly Dictionary<DicomTag, AnonFunc> _anonymisationProtocol;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymisationTagHandler"/> class.
        /// </summary>
        /// <param name="anonymisationProtocolId">The anonymisation protocol unqiue identifier.</param>
        /// <param name="anonymisationProtocol">The anonymisation protocol.</param>
        /// <exception cref="ArgumentNullException">anonymisationProtocol</exception>
        /// <exception cref="ArgumentException">Unknown DICOM anonymisation protocol</exception>
        public AnonymisationTagHandler(Guid anonymisationProtocolId, IEnumerable<DicomTagAnonymisation> anonymisationProtocol)
        {
            if (anonymisationProtocol == null)
            {
                throw new ArgumentNullException(nameof(anonymisationProtocol));
            }

            var result = new Dictionary<DicomTag, AnonFunc>();

            foreach (var dicomTag in anonymisationProtocol)
            {
                try
                {
                    switch (dicomTag.AnonymisationProtocol)
                    {
                        case AnonymisationMethod.Hash:
                            result.Add(dicomTag.DicomTagIndex.DicomTag, LongHashID);
                            break;
                        case AnonymisationMethod.Keep:
                            result.Add(dicomTag.DicomTagIndex.DicomTag, KeepItem);
                            break;
                        case AnonymisationMethod.RandomiseDateTime:
                            result.Add(dicomTag.DicomTagIndex.DicomTag, RandomiseDateTime);
                            break;
                        default:
                            throw new ArgumentException("Unknown DICOM anonymisation protocol");
                    }
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"DICOM tag {dicomTag.DicomTagIndex.DicomTag.DictionaryEntry.Name} already added", e);
                }
            }

            _anonymisationProtocol = result;
            _anonymisationProtocolId = anonymisationProtocolId;
            _assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        /// <summary>
        /// Gets the deidentification string.
        /// </summary>
        /// <value>
        /// The deidentification string.
        /// </value>
        protected string DeidentificationMethodString => string.Format(TagHandlerDeidentificationNameFormat, _assemblyVersion, _anonymisationProtocolId);

        [Description("Uses SHA512 to generate a new 64 char length Unique Identifier.")]
        public static DicomItem LongHashID(DicomDataset oldds, List<TagOrIndex> path, DicomItem item)
        {
            var el = item as DicomElement;
            Trace.Assert(el != null);

            var id = el.Get<string>();

            if (string.IsNullOrEmpty(id))
            {
                return new DicomLongString(item.Tag, string.Empty);
            }

            return new DicomLongString(item.Tag, HashingFunctions.HashID(id));
        }

        [Description("Keeps the examined item.")]
        public static DicomItem KeepItem(DicomDataset oldds, List<TagOrIndex> path, DicomItem item) => item;

        [Description(@"Adds a random offset of 0-365 days to the original date value & 0-24 hours to the time.
                       The offset is calculated by seeding a random number generator for the patient ID.
                       If either the DICOM tag value is empty or the patient ID is empty we result is set to empty.
                       Note, if this property is a time value type, this randomisation method might not preserve
                       the file ordering of scans within the same study. If the time anonymisation overflows into
                       the next day, the day field is stripped and may disrupt the ordering of files aquired on the same day.")]
        public static DicomItem RandomiseDateTime(DicomDataset oldds, List<TagOrIndex> path, DicomItem item)
        {
            var dateTime = TryGetDateTime(oldds, item.Tag);

            oldds.TryGetString(DicomTag.PatientID, out var patientId);

            if (!dateTime.HasValue || string.IsNullOrWhiteSpace(patientId))
            {
                return GetDicomDateElement(item.ValueRepresentation, item.Tag, null);
            }

            dateTime = RandomiseDate(patientId, dateTime.Value);
            dateTime = RandomiseTime(patientId, dateTime.Value);

            return GetDicomDateElement(item.ValueRepresentation, item.Tag, dateTime);
        }

        /// <summary>
        /// Example *required* by the Anonymisation Engine
        /// </summary>
        /// <returns></returns>
        public static List<AnonExample> KeepItemExamples()
        {
            var output = new AnonExample();

            var value = "M";
            var item = new DicomLongString(DicomTag.PatientSex, value);
            output.Input.Add(item + ": " + value);

            var dicomItem = KeepItem(null, null, item);

            AnonExample.InferOutput(item, dicomItem, output);

            return new List<AnonExample> { output };
        }

        /// <summary>
        /// Example *required* by the Anonymisation Engine
        /// </summary>
        /// <returns></returns>
        public static List<AnonExample> LongHashIDExamples()
        {
            var output1 = new AnonExample();

            var value = "1.2.34567";
            var item = new DicomUniqueIdentifier(DicomTag.ConcatenationUID, value);
            output1.Input.Add(item + ": " + value);

            AnonExample.InferOutput(
                item,
                LongHashID(null, null, item),
                output1);

            var output2 = new AnonExample();

            value = string.Empty;
            item = new DicomUniqueIdentifier(DicomTag.ConcatenationUID, value);
            output2.Input.Add(item + ": " + "<empty>");

            AnonExample.InferOutput(
                item,
                LongHashID(null, null, item),
                output2);

            return new List<AnonExample> { output1, output2 };
        }

        /// <summary>
        /// Example *required* by the Anonymisation Engine
        /// </summary>
        /// <returns></returns>
        public static List<AnonExample> RandomiseDateTimeExamples()
        {
            var dateTime = new DateTime(2018, 2, 15, 3, 20, 58);
            var dicomItem = new DicomDateTime(DicomTag.CreationDate, dateTime);

            var dataset = new DicomDataset()
            {
                { DicomTag.PatientID, "TEST" },
                { dicomItem },
            };

            var output = new AnonExample();
            output.Input.Add(dicomItem + ": " + dateTime);

            AnonExample.InferOutput(dicomItem, RandomiseDateTime(dataset, null, dicomItem), output);

            return new List<AnonExample> { output };
        }

        // TODO refactor into abstract class to avoid all this useless code
        public Dictionary<string, string> GetConfiguration() => null;

        // TODO refactor into abstract class to avoid all this useless code
        public Dictionary<Regex, AnonFunc> GetRegexFuncs() => null;

        // TODO refactor into abstract class to avoid all this useless code
        public Dictionary<DicomTag, AnonFunc> GetTagFuncs() => _anonymisationProtocol;

        // TODO refactor into abstract class to avoid all this useless code
        public void NextDataset()
        {
        }

        /// <summary>
        /// Post-process step for the anonymised DICOM dataset.
        /// </summary>
        /// <param name="newds">The DICOM dataset.</param>
        public void Postprocess(DicomDataset newds)
        {
            if (newds == null)
            {
                throw new ArgumentNullException(nameof(newds));
            }

            var values = Array.Empty<string>();
            newds.TryGetValues(DicomTag.DeidentificationMethod, out values);

            var dicomDatasetDeidentificationMethods = values?.ToList() ?? new List<string>();

            // The VR is LO - therefore chopping this down to 64 characters
            dicomDatasetDeidentificationMethods.Add(DeidentificationMethodString.Substring(0, Math.Min(DeidentificationMethodString.Length, 64)));

            newds.AddOrUpdate(DicomTag.DeidentificationMethod, dicomDatasetDeidentificationMethods.ToArray());
            newds.AddOrUpdate(DicomTag.PatientIdentityRemoved, "YES");
            newds.AddOrUpdate(DicomTag.LongitudinalTemporalInformationModified, "MODIFIED");
        }

        /// <summary>
        /// Tries to get the date time from a DICOM element.
        /// </summary>
        /// <param name="dicomDataset">A DICOM dataset</param>
        /// <param name="dicomTag">Tag to fetch from the dataset</param>
        /// <returns>The date time.</returns>
        private static DateTime? TryGetDateTime(DicomDataset dicomDataset, DicomTag dicomTag)
        {
            if (dicomDataset == null || dicomTag == null)
            {
                return null;
            }

            try
            {
                return dicomDataset.GetSingleValue<DateTime>(dicomTag);
            }

            // Tag value null.
            catch (DicomDataException)
            {
            }

            // Tag value is not null but has an invalid format that cannot be parsed into a DateTime object.
            catch (FormatException)
            {
            }

            return null;
        }

        /// <summary>
        /// Randomises the time of a date time property using the patient ID as a random number seed.
        /// The date time is randomised by adding a number (between 0 and timeMaximumOffsetInHours * 3600) seconds.
        /// Note: This method may not preserve to the continuity of data within the same time period. If this time overflows
        /// into the next day, the day field is stripped when saving a DICOM time tag. This will disrupt the ordering of files
        /// within a study scanned on the same day.
        /// </summary>
        /// <param name="patientId">The patient identifier.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="timeMaximumOffsetInHours">The time maximum offset in hours.</param>
        /// <returns>The randomised date time.</returns>
        private static DateTime RandomiseTime(string patientId, DateTime dateTime, int timeMaximumOffsetInHours = 24)
        {
            var random = CreateSeededRandom(patientId);
            var secondsOffset = random.Next(0, timeMaximumOffsetInHours * 3600);

            return dateTime.AddSeconds(secondsOffset);
        }

        /// <summary>
        /// Randomises the date of date time property using the patient ID as a random number seed.
        /// The date is randomised by adding a number between 0 and dateMaximumOffsetInDays days.
        /// </summary>
        /// <param name="patientId">The patient identifier.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="dateMaximumOffsetInDays">The date maximum offset in days.</param>
        /// <returns>The randomised date time.</returns>
        private static DateTime RandomiseDate(string patientId, DateTime dateTime, int dateMaximumOffsetInDays = 365)
        {
            var random = CreateSeededRandom(patientId);
            var daysOffset = random.Next(0, dateMaximumOffsetInDays);

            try
            {
                return dateTime.AddDays(daysOffset);
            }
            catch (ArgumentException)
            {
                return dateTime.AddDays(-daysOffset);
            }
        }

        /// <summary>
        /// Creates a random number generator seeded by the patient ID.
        /// </summary>
        /// <param name="patientId">The patient identifier.</param>
        /// <returns>The seeded random number generator.</returns>
        private static Random CreateSeededRandom(string patientId)
        {
            return new Random(patientId.Select(x => (int)x).Sum());
        }

        /// <summary>
        /// Gets the correct DICOM date element for the specified DICOM value representation.
        /// </summary>
        /// <param name="valueRepresentation">The DICOM value representation.</param>
        /// <param name="dicomTag">The dicom tag.</param>
        /// <param name="dateTime">The date time.</param>
        /// <returns>The correct DICOM date element.</returns>
        /// <exception cref="ArgumentException">If the DICOM value representaiton is not a DICOM date element.</exception>
        private static DicomDateElement GetDicomDateElement(DicomVR valueRepresentation, DicomTag dicomTag, DateTime? dateTime)
        {
            if (valueRepresentation == DicomVR.TM)
            {
                return dateTime.HasValue ? new DicomTime(dicomTag, dateTime.Value) : new DicomTime(dicomTag, string.Empty);
            }
            else if (valueRepresentation == DicomVR.DA)
            {
                return dateTime.HasValue ? new DicomDate(dicomTag, dateTime.Value) : new DicomDate(dicomTag, string.Empty);
            }
            else if (valueRepresentation == DicomVR.DT)
            {
                return dateTime.HasValue ? new DicomDateTime(dicomTag, dateTime.Value) : new DicomDateTime(dicomTag, string.Empty);
            }

            throw new ArgumentException($"Dicom Tag {dicomTag} is of an unknown value representation to anonymise the date time. VR: {valueRepresentation}");
        }
    }
}