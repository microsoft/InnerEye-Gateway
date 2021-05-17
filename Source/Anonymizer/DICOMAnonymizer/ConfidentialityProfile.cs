// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace DICOMAnonymizer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Dicom;
    using AnonFunc = System.Func<Dicom.DicomDataset, System.Collections.Generic.List<TagOrIndex>, Dicom.DicomItem, Dicom.DicomItem>;

    public class ConfidentialityProfile : ITagHandler
    {

        #region DICOM Confidentiality Profile

        public static readonly IReadOnlyList<string> TagProfile = new List<string>
        {
            "0008,0050;Z;;;;;;;;;",
            "0018,4000;X;;;;;;;C;;",
            "0040,0555;X;;;;;;;;C;",
            "0008,0022;X/Z;;;;;K;C;;;",
            "0008,002A;X/D;;;;;K;C;;;",
            "0018,1400;X/D;;;;;;;C;;",
            "0018,9424;X;;;;;;;C;;",
            "0008,0032;X/Z;;;;;K;C;;;",
            "0040,4035;X;;;;;;;;;",
            "0010,21B0;X;;;;;;;C;;",
            "0040,A353;X;;;;;;;;;",
            "0038,0010;X;;;;;;;;;",
            "0038,0020;X;;;;;K;C;;;",
            "0008,1084;X;;;;;;;C;;",
            "0008,1080;X;;;;;;;C;;",
            "0038,0021;X;;;;;K;C;;;",
            "0000,1000;X;;K;;;;;;;",
            "0010,2110;X;;;;C;;;C;;",
            "4000,0010;X;;;;;;;;;",
            "0040,A078;X;;;;;;;;;",
            "0010,1081;X;;;;;;;;;",
            "0018,1007;X;;;K;;;;;;",
            "0040,0280;X;;;;;;;C;;",
            "0020,9161;U;;K;;;;;;;",
            "0040,3001;X;;;;;;;;;",
            "0008,009D;X;;;;;;;;;",
            "0008,009C;Z;;;;;;;;;",
            "0070,0084;Z;;;;;;;;;",
            "0070,0086;X;;;;;;;;;",
            "0008,0023;Z/D;;;;;K;C;;;",
            "0040,A730;X;;;;;;;;C;",
            "0008,0033;Z/D;;;;;K;C;;;",
            "0018,0010;Z/D;;;;;;;C;;",
            "0018,A003;X;;;;;;;C;;",
            "0010,2150;X;;;;;;;;;",
            "0040,A307;X;;;;;;;;;",
            "0038,0300;X;;;;;;;;;",
            "0008,0025;X;;;;;K;C;;;",
            "0008,0035;X;;;;;K;C;;;",
            "0040,A07C;X;;;;;;;;;",
            "FFFC,FFFC;X;;;;;;;;;",
            "0008,2111;X;;;;;;;C;;",
            "0018,700A;X/D;;;K;;;;;;",
            "0018,1000;X/Z/D;;;K;;;;;;",
            "0018,1002;U;;K;K;;;;;;",
            "0400,0100;X;;;;;;;;;",
            "FFFA,FFFA;X;;;;;;;;;",
            "0020,9164;U;;K;;;;;;;",
            "0038,0040;X;;;;;;;C;;",
            "4008,011A;X;;;;;;;;;",
            "4008,0119;X;;;;;;;;;",
            "300A,0013;U;;K;;;;;;;",
            "0018,9517;X/D;;;;;K;C;;;",
            "0010,2160;X;;;;K;;;;;",
            "0040,4011;X;;;;;K;C;;;",
            "0008,0058;U;;K;;;;;;;",
            "0070,031A;U;;K;;;;;;;",
            "0040,2017;Z;;;;;;;;;",
            "0020,9158;X;;;;;;;C;;",
            "0020,0052;U;;K;;;;;;;",
            "0018,1008;X;;;K;;;;;;",
            "0018,1005;X;;;K;;;;;;",
            "0070,0001;D;;;;;;;;;C",
            "0040,4037;X;;;;;;;;;",
            "0040,4036;X;;;;;;;;;",
            "0088,0200;X;;;;;;;;;",
            "0008,4000;X;;;;;;;C;;",
            "0020,4000;X;;;;;;;C;;",
            "0028,4000;X;;;;;;;;;",
            "0040,2400;X;;;;;;;C;;",
            "4008,0300;X;;;;;;;C;;",
            "0008,0015;X;;;;;K;C;;;",
            "0008,0014;U;;K;;;;;;;",
            "0008,0081;X;;;;;;;;;",
            "0008,0082;X/Z/D;;;;;;;;;",
            "0008,0080;X/Z/D;;;;;;;;;",
            "0008,1040;X;;;;;;;;;",
            "0010,1050;X;;;;;;;;;",
            "0040,1011;X;;;;;;;;;",
            "4008,0111;X;;;;;;;;;",
            "4008,010C;X;;;;;;;;;",
            "4008,0115;X;;;;;;;C;;",
            "4008,0202;X;;;;;;;;;",
            "4008,0102;X;;;;;;;;;",
            "4008,010B;X;;;;;;;C;;",
            "4008,010A;X;;;;;;;;;",
            "0008,3010;U;;K;;;;;;;",
            "0038,0011;X;;;;;;;;;",
            "0010,0021;X;;;;;;;;;",
            "0038,0061;X;;;;;;;;;",
            "0028,1214;U;;K;;;;;;;",
            "0010,21D0;X;;;;;K;C;;;",
            "0400,0404;X;;;;;;;;;",
            "0002,0003;U;;K;;;;;;;",
            "0010,2000;X;;;;;;;C;;",
            "0010,1090;X;;;;;;;;;",
            "0010,1080;X;;;;;;;;;",
            "0400,0550;X;;;;;;;;;",
            "0020,3406;X;;;;;;;;;",
            "0020,3401;X;;;;;;;;;",
            "0020,3404;X;;;;;;;;;",
            "0008,1060;X;;;;;;;;;",
            "0040,1010;X;;;;;;;;;",
            "0040,A192;X;;;;;K;C;;;",
            "0040,A402;U;;K;;;;;;;",
            "0040,A193;X;;;;;K;C;;;",
            "0040,A171;U;;K;;;;;;;",
            "0010,2180;X;;;;;;;C;;",
            "0008,1072;X/D;;;;;;;;;",
            "0008,1070;X/Z/D;;;;;;;;;",
            "0400,0561;X;;;;;;;;;",
            "0040,2010;X;;;;;;;;;",
            "0040,2011;X;;;;;;;;;",
            "0040,2008;X;;;;;;;;;",
            "0040,2009;X;;;;;;;;;",
            "0010,1000;X;;;;;;;;;",
            "0010,1002;X;;;;;;;;;",
            "0010,1001;X;;;;;;;;;",
            "0008,0024;X;;;;;K;C;;;",
            "0008,0034;X;;;;;K;C;;;",
            "0028,1199;U;;K;;;;;;;",
            "0040,A07A;X;;;;;;;;;",
            "0010,1040;X;;;;;;;;;",
            "0010,4000;X;;;;;;;C;;",
            "0010,0020;Z;;;;;;;;;",
            "0010,2203;X/Z;;;;K;;;;;",
            "0038,0500;X;;;;C;;;C;;",
            "0040,1004;X;;;;;;;;;",
            "0010,1010;X;;;;K;;;;;",
            "0010,0030;Z;;;;;;;;;",
            "0010,1005;X;;;;;;;;;",
            "0010,0032;X;;;;;;;;;",
            "0038,0400;X;;;;;;;;;",
            "0010,0050;X;;;;;;;;;",
            "0010,1060;X;;;;;;;;;",
            "0010,0010;Z;;;;;;;;;",
            "0010,0101;X;;;;;;;;;",
            "0010,0102;X;;;;;;;;;",
            "0010,21F0;X;;;;;;;;;",
            "0010,0040;Z;;;;K;;;;;",
            "0010,1020;X;;;;K;;;;;",
            "0010,2155;X;;;;;;;;;",
            "0010,2154;X;;;;;;;;;",
            "0010,1030;X;;;;K;;;;;",
            "0040,0243;X;;;;;;;;;",
            "0040,0254;X;;;;;;;C;;",
            "0040,0250;X;;;;;K;C;;;",
            "0040,4051;X;;;;;K;C;;;",
            "0040,0251;X;;;;;K;C;;;",
            "0040,0253;X;;;;;;;;;",
            "0040,0244;X;;;;;K;C;;;",
            "0040,4050;X;;;;;K;C;;;",
            "0040,0245;X;;;;;K;C;;;",
            "0040,0241;X;;;K;;;;;;",
            "0040,4030;X;;;K;;;;;;",
            "0040,0242;X;;;K;;;;;;",
            "0040,4028;X;;;K;;;;;;",
            "0008,1052;X;;;;;;;;;",
            "0008,1050;X;;;;;;;;;",
            "0040,1102;X;;;;;;;;;",
            "0040,1101;D;;;;;;;;;",
            "0040,A123;D;;;;;;;;;",
            "0040,1104;X;;;;;;;;;",
            "0040,1103;X;;;;;;;;;",
            "4008,0114;X;;;;;;;;;",
            "0008,1062;X;;;;;;;;;",
            "0008,1048;X;;;;;;;;;",
            "0008,1049;X;;;;;;;;;",
            "0040,2016;Z;;;;;;;;;",
            "0018,1004;X;;;K;;;;;;",
            "0040,0012;X;;;;C;;;;;",
            "0010,21C0;X;;;;K;;;;;",
            "0070,1101;U;;K;;;;;;;",
            "0070,1102;U;;K;;;;;;;",
            "0040,4052;X;;;;;K;C;;;",
            "0018,1030;X/D;;;;;;;C;;",
            "300C,0113;X;;;;;;;C;;",
            "0040,2001;X;;;;;;;C;;",
            "0032,1030;X;;;;;;;C;;",
            "0400,0402;X;;;;;;;;;",
            "3006,0024;U;;K;;;;;;;",
            "0040,4023;U;;K;;;;;;;",
            "0008,1140;X/Z/U*;;K;;;;;;;",
            "0040,A172;U;;K;;;;;;;",
            "0038,0004;X;;;;;;;;;",
            "0010,1100;X;;;;;;;;;",
            "0008,1120;X;;X;;;;;;;",
            "0008,1111;X/Z/D;;K;;;;;;;",
            "0400,0403;X;;;;;;;;;",
            "0008,1155;U;;K;;;;;;;",
            "0004,1511;U;;K;;;;;;;",
            "0008,1110;X/Z;;K;;;;;;;",
            "0008,0092;X;;;;;;;;;",
            "0008,0096;X;;;;;;;;;",
            "0008,0090;Z;;;;;;;;;",
            "0008,0094;X;;;;;;;;;",
            "0010,2152;X;;;;;;;;;",
            "3006,00C2;U;;K;;;;;;;",
            "0040,0275;X;;;;;;;C;;",
            "0032,1070;X;;;;;;;C;;",
            "0040,1400;X;;;;;;;C;;",
            "0032,1060;X/Z;;;;;;;C;;",
            "0040,1001;X;;;;;;;;;",
            "0040,1005;X;;;;;;;;;",
            "0000,1001;U;;K;;;;;;;",
            "0032,1032;X;;;;;;;;;",
            "0032,1033;X;;;;;;;;;",
            "0010,2299;X;;;;;;;;;",
            "0010,2297;X;;;;;;;;;",
            "4008,4000;X;;;;;;;C;;",
            "4008,0118;X;;;;;;;;;",
            "4008,0042;X;;;;;;;;;",
            "300E,0008;X/Z;;;;;;;;;",
            "0040,4034;X;;;;;;;;;",
            "0038,001E;X;;;;;;;;;",
            "0040,000B;X;;;;;;;;;",
            "0040,0006;X;;;;;;;;;",
            "0040,0004;X;;;;;K;C;;;",
            "0040,0005;X;;;;;K;C;;;",
            "0040,0007;X;;;;;;;C;;",
            "0040,0011;X;;;K;;;;;;",
            "0040,4010;X;;;;;K;C;;;",
            "0040,0002;X;;;;;K;C;;;",
            "0040,4005;X;;;;;K;C;;;",
            "0040,0003;X;;;;;K;C;;;",
            "0040,0001;X;;;K;;;;;;",
            "0040,4027;X;;;K;;;;;;",
            "0040,0010;X;;;K;;;;;;",
            "0040,4025;X;;;K;;;;;;",
            "0032,1020;X;;;K;;;;;;",
            "0032,1021;X;;;K;;;;;;",
            "0008,0021;X/D;;;;;K;C;;;",
            "0008,103E;X;;;;;;;C;;",
            "0020,000E;U;;K;;;;;;;",
            "0008,0031;X/D;;;;;K;C;;;",
            "0038,0062;X;;;;;;;C;;",
            "0038,0060;X;;;;;;;;;",
            "0010,21A0;X;;;;K;;;;;",
            "0008,0018;U;;K;;;;;;;",
            "0008,2112;X/Z/U*;;K;;;;;;;",
            "3008,0105;X;;;K;;;;;;",
            "0038,0050;X;;;;C;;;;;",
            "0018,9516;X/D;;;;;K;C;;;",
            "0008,1010;X/Z/D;;;K;;;;;;",
            "0088,0140;U;;K;;;;;;;",
            "0032,4000;X;;;;;;;C;;",
            "0008,0020;Z;;;;;K;C;;;",
            "0008,1030;X;;;;;;;C;;",
            "0020,0010;Z;;;;;;;;;",
            "0032,0012;X;;;;;;;;;",
            "0020,000D;U;;K;;;;;;;",
            "0008,0030;Z;;;;;K;C;;;",
            "0020,0200;U;;K;;;;;;;",
            "0018,2042;U;;K;;;;;;;",
            "0040,A354;X;;;;;;;;;",
            "0040,DB0D;U;;K;;;;;;;",
            "0040,DB0C;U;;K;;;;;;;",
            "4000,4000;X;;;;;;;;;",
            "2030,0020;X;;;;;;;;;",
            "0008,0201;X;;;;;K;C;;;",
            "0088,0910;X;;;;;;;;;",
            "0088,0912;X;;;;;;;;;",
            "0088,0906;X;;;;;;;;;",
            "0088,0904;X;;;;;;;;;",
            "0062,0021;U;;K;;;;;;;",
            "0008,1195;U;;K;;;;;;;",
            "0040,A124;U;;;;;;;;;",
            "0040,A352;X;;;;;;;;;",
            "0040,A358;X;;;;;;;;;",
            "0040,A088;Z;;;;;;;;;",
            "0040,A075;D;;;;;;;;;",
            "0040,A073;D;;;;;;;;;",
            "0040,A027;X;;;;;;;;;",
            "0038,4000;X;;;;;;;C;;",
        };

        public static readonly IReadOnlyList<string> RegexProfile = new List<string>
        {
            "[0-9A-F]{3}[13579BDF],[0-9A-F]{4};X;C;;;;;;;;",
            "50[0-9A-F]{2},[0-9A-F]{4};X;;;;;;;;;C",
            "60[0-9A-F]{2},4000;X;;;;;;;;;C",
            "60[0-9A-F]{2},3000;X;;;;;;;;;C",
        };

        #endregion

        #region Options

        /// <summary>Profile options as described in DICOM PS 3.15-2017c</summary>
        /// <see>http://dicom.nema.org/medical/dicom/current/output/html/part15.html</see>
        /// <remarks>The order of the flags are mapped to the profile's CSV file</remarks>
        [Flags]
#pragma warning disable CA1028 // Enum Storage should be Int32
        public enum SecurityProfileOptions : short
#pragma warning restore CA1028 // Enum Storage should be Int32
        {
            BasicProfile = 1,
            RetainSafePrivate = 2,
            RetainUIDs = 4,
            RetainDeviceIdent = 8,
            RetainPatientChars = 16,
            RetainLongFullDates = 32,
            RetainLongModifDates = 64,
            CleanDesc = 128,
            CleanStructdCont = 256,
            CleanGraph = 512,
        }

        /// <summary>Profile actions per tag as described in DICOM PS 3.15-2017c</summary>
        /// <see>http://dicom.nema.org/medical/dicom/current/output/html/part15.html</see>
#pragma warning disable CA1028 // Enum Storage should be Int32
        [Flags]
        public enum SecurityProfileActions : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
        {
            D = 1,  // Replace with a non-zero length value that may be a dummy value and consistent with the VR
            Z = 2,  // Replace with a zero length value, or a non-zero length value that may be a dummy value and consistent with the VR
            X = 4,  // Remove
            K = 8,  // Keep (unchanged for non-sequence attributes, cleaned for sequences)
            C = 16, // Clean, that is replace with values of similar meaning known not to contain identifying information and consistent with the VR
            U = 32, // Replace with a non-zero length UID that is internally consistent within a set of Instances            
        }

        #endregion

        private static readonly int _optionsCount = Enum.GetValues(typeof(SecurityProfileOptions)).Length;
        private readonly SecurityProfileOptions _options;

        #region Helper methods

        /// <summary>
        /// Return the tag and the appropriate action based on the security profile
        /// </summary>
        /// <param name="item">A valid confidentiality profile line</param>
        /// <param name="options">The security profile options</param>
        /// <returns></returns>
        private Tuple<string, AnonFunc> ParseProfileItem(string item, SecurityProfileOptions options)
        {
            SecurityProfileActions? action = null;
            var parts = item.Split(';');
            var tag = parts[0];

            for (var i = 0; i < _optionsCount; i++)
            {
                var flag = (SecurityProfileOptions)(1 << i);

                if ((options & flag) == flag)
                {
                    var a = parts[i + 1].ToCharArray().FirstOrDefault().ToString();
                    if (a != default(char).ToString())
                    {
                        action = (SecurityProfileActions)Enum.Parse(typeof(SecurityProfileActions), a);
                    }
                }
            }

            switch (action.Value)
            {
                case SecurityProfileActions.U: // UID
                    return Tuple.Create<string, AnonFunc>(tag, ReplaceUID);
                case SecurityProfileActions.C: // Clean
                case SecurityProfileActions.D: // Dummy
                    return Tuple.Create<string, AnonFunc>(tag, CleanDummyElement);
                case SecurityProfileActions.K: // Keep
                    return Tuple.Create<string, AnonFunc>(tag, KeepItem);
                case SecurityProfileActions.X: // Remove
                    return Tuple.Create<string, AnonFunc>(tag, RemoveItem);
                case SecurityProfileActions.Z: // Zero-length
                    return Tuple.Create<string, AnonFunc>(tag, BlankElement);
                default:
                    throw new ArgumentOutOfRangeException($"Unrecognized action: {action.Value}");
            }
        }

        #endregion

        #region Constructors

        public ConfidentialityProfile() : this(SecurityProfileOptions.BasicProfile) { }

        public ConfidentialityProfile(SecurityProfileOptions ops)
        {
            _options = ops;
        }

        #endregion

        public Dictionary<Regex, AnonFunc> GetRegexFuncs()
        {
            var regexActions = new Dictionary<Regex, AnonFunc>();

            if (regexActions.Count == 0)
            {
                foreach (var item in RegexProfile)
                {
                    var t = ParseProfileItem(item, _options);
                    var tag = new Regex(t.Item1, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    regexActions[tag] = t.Item2;
                }
            }

            return regexActions;
        }

        public Dictionary<DicomTag, AnonFunc> GetTagFuncs()
        {
            var tagActions = new Dictionary<DicomTag, AnonFunc>();

            if (tagActions.Count == 0)
            {
                foreach (var item in TagProfile)
                {
                    var t = ParseProfileItem(item, _options);
                    var tag = DicomTag.Parse(t.Item1);

                    tagActions[tag] = t.Item2;
                }
            }

            return tagActions;
        }

        public Dictionary<string, string> GetConfiguration()
        {
            return new Dictionary<string, string>
            {
                {"Security Profile Options", _options.ToString() },
            };
        }

        public void NextDataset() { }

        public void Postprocess(DicomDataset newds) { }

        #region Tag handler helpers

        /// <summary>
        /// Use reflection to get strongly-typed constructor info from <paramref name="element"/>.
        /// </summary>
        /// <param name="element">DICOM element for which constructor info should be obtained.</param>
        /// <param name="parameterTypes">Expected parameter types in the requested constructor.</param>
        /// <returns>Constructor info corresponding to <paramref name="element"/> and <paramref name="parameterTypes"/>.</returns>
        private static ConstructorInfo GetConstructor(DicomElement element, params Type[] parameterTypes)
        {
            return element.GetType().GetConstructor(parameterTypes);
        }

        /// <summary>Evaluates whether an element is of type Other*</summary>
        /// <param name="element">The element to be evaluated</param>
        /// <returns>A boolean flag indicating whether the element is of the expected type, otherwise false</returns>
        public static bool IsOtherElement(DicomElement element)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            var t = element.GetType();
            return t == typeof(DicomOtherByte) || t == typeof(DicomOtherDouble) || t == typeof(DicomOtherFloat)
                   || t == typeof(DicomOtherLong) || t == typeof(DicomOtherWord) || t == typeof(DicomUnknown);
        }

        /// <summary>Evaluates whether an element has a generic valueType</summary>
        /// <param name="element">The element to be evaluated</param>
        /// <returns>The data type if found, otherwise null</returns>
        public static Type ElementValueType(DicomElement element)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            var t = element.GetType();
            if (t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(DicomValueElement<>))
            {
                return t.GenericTypeArguments[0];
            }

            return null;
        }

        #endregion

        #region Tag handlers

        // TODO: Reset every new study?
        /// <summary>Context/Output. Contains all the replaced UIDs.</summary>
        /// <remarks>Useful for consistency across a file set (multiple calls to anonymization methods)</remarks>
        public Dictionary<string, string> ReplacedUIDs { get; } = new Dictionary<string, string>();

        /// <string>Replaces the content of a UID with a random one</string>
        /// <param name="oldds">Reference to the old dataset</param>
        /// <param name="newds">Reference to the new dataset</param>
        /// <param name="element">The element to be altered</param>
        [Description(@"We replace a UID with a new random one. This function keeps internal state so
                       the same UID tag in future Dicoms will be consistently replaced. To reset,
                       use the ReplacedUIDs property.")]
        public DicomItem ReplaceUID(DicomDataset oldds, IReadOnlyList<TagOrIndex> path, DicomItem item)
        {
            item = item ?? throw new ArgumentNullException(nameof(item));

#pragma warning disable CA1508 // Avoid dead conditional code
            if (!(item is DicomElement element))
#pragma warning restore CA1508 // Avoid dead conditional code
            {
                throw new InvalidOperationException($"ReplaceUID can not handle type: {item.GetType()}");
            }

            if (element.ValueRepresentation != DicomVR.UI)
            {
                throw new ArgumentOutOfRangeException($"Tag: {element.Tag} is marked as U but VR is: {element.ValueRepresentation}");
            }

            string rep;
            DicomUID uid;
            var old = element.Get<string>();

            if (ReplacedUIDs.ContainsKey(old))
            {
                rep = ReplacedUIDs[old];
                uid = new DicomUID(rep, "Anonymized UID", DicomUidType.Unknown);
            }
            else
            {
                uid = DicomUIDGenerator.GenerateDerivedFromUUID();
                rep = uid.UID;
                ReplacedUIDs[old] = rep;
            }

            return new DicomUniqueIdentifier(element.Tag, uid);
        }

        [Description(@"Keeps the examined item but sets its value to be a dummy value. If the VR is a string
                       we replace it with the word 'ANONYMOUS', or else we set it to be empty.")]
        public static DicomItem CleanDummyElement(DicomDataset oldds, IReadOnlyList<TagOrIndex> path, DicomItem item)
        {
            item = item ?? throw new ArgumentNullException(nameof(item));

            var vr = item.ValueRepresentation;

            if (vr.IsString)
            {
                // TODO: Needed to create a DicomItem with the correct VR
                return new DicomDataset().AddOrUpdate(item.Tag, "ANONYMOUS").First();
            }
            else
            {
                return BlankElement(oldds, path, item);
            }
        }

        /// <summary>Blanks an item by passing to it an empty string</summary>
        /// <param name="oldds">Reference to the old dataset</param>
        /// <param name="item">The element to be processed</param>
        [Description("Keeps the examined item but sets its value to be empty.")]
        public static DicomItem BlankElement(DicomDataset oldds, IReadOnlyList<TagOrIndex> path, DicomItem item)
        {
            if (!(item is DicomElement element))
            {
                return item;
            }

            // Special date/time cases
            if (element is DicomDateTime)
            {
                return new DicomDateTime(element.Tag, DateTime.MinValue);
            }
            if (element is DicomDate)
            {
                return new DicomDate(element.Tag, DateTime.MinValue);
            }
            if (element is DicomTime)
            {
                return new DicomTime(element.Tag, new DicomDateRange());
            }

            if (element is DicomStringElement)
            {
                // TODO: Needed to create a DicomItem with the correct VR
                return new DicomDataset().AddOrUpdate(element.Tag, string.Empty).First();
            }

            if (IsOtherElement(element)) // Replaces with an empty array
            {
                var ctor = GetConstructor(element, typeof(DicomTag));
                var t = (DicomItem)ctor.Invoke(new object[] { element.Tag });

                // TODO: Needed to create a DicomItem with the correct VR
                return new DicomDataset().AddOrUpdate(element.Tag, t).First();
            }

            var valueType = ElementValueType(element); // Replace with the default value
            if (valueType != null)
            {
                var ctor = GetConstructor(element, typeof(DicomTag), valueType);
                var t = (DicomItem)ctor.Invoke(new[] { element.Tag, Activator.CreateInstance(valueType) });

                // TODO: Needed to create a DicomItem with the correct VR
                return new DicomDataset().AddOrUpdate(element.Tag, t).First();
            }

            throw new InvalidOperationException($"Missed type: {item.GetType()}");
        }

        /// <summary>
        /// Removes an item by not adding it to the new dataset
        /// </summary>
        /// <param name="oldds"></param>
        /// <param name="item"></param>
        [Description("Removes the examined item.")]
        public static DicomItem RemoveItem(DicomDataset oldds, IReadOnlyList<TagOrIndex> path, DicomItem item) { return null; }

        /// <summary>
        /// Keeps an item by adding it to the new dataset
        /// </summary>
        /// <param name="oldds">Reference to the old dataset</param>
        /// <param name="item">The element to be processed</param>
        [Description("Keeps the examined item.")]
        public static DicomItem KeepItem(DicomDataset oldds, IReadOnlyList<TagOrIndex> path, DicomItem item) { return item; }

        #endregion

        #region Tag handler examples

        public static IReadOnlyList<AnonExample> ReplaceUIDExamples()
        {
            return new List<AnonExample> { };
        }

        public static IReadOnlyList<AnonExample> CleanDummyElementExamples()
        {
            return new List<AnonExample> { };
        }

        public static IReadOnlyList<AnonExample> BlankElementExamples()
        {
            return new List<AnonExample> { };
        }

        public static IReadOnlyList<AnonExample> RemoveItemExamples()
        {
            var output = new AnonExample();

            var v = "Lorem Ipsum";
            var item = new DicomShortString(DicomTag.PerformedLocation, v);
            output.Input.Add(item + ": " + v);

            var ans = RemoveItem(null, null, item);

            AnonExample.InferOutput(item, ans, output);

            return new List<AnonExample> { output };
        }

        public static IReadOnlyList<AnonExample> KeepItemExamples()
        {
            var output = new AnonExample();

            var v = "24.23";
            var item = new DicomDecimalString(DicomTag.PatientWeight, v);
            output.Input.Add(item + ": " + v);

            var ans = KeepItem(null, null, item);

            AnonExample.InferOutput(item, ans, output);

            return new List<AnonExample> { output };
        }

        #endregion

    }
}
