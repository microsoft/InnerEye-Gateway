namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.DicomConstraints;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [TestClass]
    public class ConfigurationProviderTests : BaseTestClass
    {
        /// <summary>
        /// Save a data structure of type T to filename in folder.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="t">Instance of T.</param>
        /// <param name="folder">Folder path to serialise to.</param>
        /// <param name="filename">Filename to serialise to.</param>
        /// <param name="prettyPrint">True to format the JSON in a human readable way.</param>
        public static void Serialise<T>(T t, string folder, string filename, bool prettyPrint = false)
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                Converters = new[] { new StringEnumConverter() },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            var path = Path.Combine(folder, filename);
            var jsonText = prettyPrint ? JsonConvert.SerializeObject(t, serializerSettings) : JsonConvert.SerializeObject(t);
            File.WriteAllText(path, jsonText);
        }

        /// <summary>
        /// List of chars to use for random string generation.
        /// </summary>
        public const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// Generate a random bool.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random bool.</returns>
        public static bool RandomBool(Random random) => random.Next(2) == 1;

        /// <summary>
        /// Generate a random enum.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="random">Random.</param>
        /// <returns>Random element of enum T.</returns>
        public static T RandomEnum<T>(Random random)
        {
            var values = Enum.GetValues(typeof(T));

            return (T)values.GetValue(random.Next(values.Length));
        }

        /// <summary>
        /// Generate a random string of target length.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="length">Target string length.</param>
        /// <returns>Random string.</returns>
        public static string RandomString(Random random, int length = 6)
        {
            var s = new StringBuilder(length);

            for (var i = 0; i < length; i++)
            {
                s.Append(chars[random.Next(chars.Length)]);
            }

            return s.ToString();
        }

        /// <summary>
        /// Generate a random unsigned short.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random unsigned short.</returns>
        public static ushort RandomUShort(Random random) =>
            (ushort)random.Next(0, 65535);

        /// <summary>
        /// Generate random list of <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T">Array type.</typeparam>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <param name="count">Count of models to create.</param>
        /// <param name="createRandomT">Callback to creat</param>
        /// <returns>New list of ModelConstraintsConfig.</returns>
        public static T[] RandomArray<T>(Random random, int maxDepth, int count, Func<Random, int, T> createRandomT)
        {
            var list = new T[count];

            for (var i = 0; i < count; i++)
            {
                list[i] = createRandomT(random, maxDepth);
            }

            return list;
        }

        /// <summary>
        /// Pick a random function from a list and invoke it.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <param name="createRandomTs">List of functions taking Random, returning T.</param>
        /// <returns>New random T.</returns>
        public static T RandomItem<T>(Random random, int maxDepth, Func<Random, int, T>[] createRandomTs) =>
            createRandomTs[random.Next(0, createRandomTs.Length)].Invoke(random, maxDepth);

        /// <summary>
        /// Generate random <see cref="ServiceSettings"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random ServiceSettings.</returns>
        public static ServiceSettings RandomServiceSettings(Random random) =>
            new ServiceSettings(RandomBool(random));

        /// <summary>
        /// Generate random <see cref="DicomEndPoint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DicomEndPoint.</returns>
        public static DicomEndPoint RandomDicomEndPoint(Random random) =>
            new DicomEndPoint(
                RandomString(random, 10),
                random.Next(101, 1000),
                RandomString(random, 12));

        /// <summary>
        /// Generate random accepted Sop classes and transfer syntaxes.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="keyCount">Key count to generate.</param>
        /// <param name="valueCount">Value count per key to generate.</param>
        /// <returns>Dictionary of string to string array.</returns>
        public static Dictionary<string, string[]> RandomAcceptedSopClassesAndTransferSyntaxes(
            Random random, int keyCount = 3, int valueCount = 5)
        {
            var dictionary = new Dictionary<string, string[]>();

            for (var key = 0; key < keyCount; key++)
            {
                var list = new string[valueCount];

                for (var value = 0; value < valueCount; value++)
                {
                    list[value] = RandomString(random);
                }

                dictionary.Add(RandomString(random, 5), list);
            }

            return dictionary;
        }

        /// <summary>
        /// Generate random <see cref="ReceiveServiceConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random ReceiveServiceConfig.</returns>
        public static ReceiveServiceConfig RandomReceiveServiceConfig(Random random) =>
            new ReceiveServiceConfig(
                RandomDicomEndPoint(random),
                RandomString(random, 24),
                RandomAcceptedSopClassesAndTransferSyntaxes(random));

        /// <summary>
        /// Generate random <see cref="ConfigurationServiceConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random ConfigurationServiceConfig.</returns>
        public static ConfigurationServiceConfig RandomConfigurationServiceConfig(Random random) =>
            new ConfigurationServiceConfig(
                DateTime.UtcNow.AddSeconds(5),
                DateTime.UtcNow.AddSeconds(10),
                random.Next(61, 3600));

        /// <summary>
        /// Generate random <see cref="GatewayReceiveConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random GatewayReceiveConfig.</returns>
        public static GatewayReceiveConfig RandomGatewayReceiveConfig(Random random) =>
            new GatewayReceiveConfig(
                RandomServiceSettings(random),
                RandomReceiveServiceConfig(random),
                RandomConfigurationServiceConfig(random));

        /// <summary>
        /// Generate random <see cref="ProcessorSettings"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random ProcessorSettings.</returns>
        public static ProcessorSettings RandomProcessorSettings(Random random) =>
            new ProcessorSettings(
                RandomString(random, 12),
                new Uri("https://" + RandomString(random, 8) + ".com"));

        /// <summary>
        /// Generate random <see cref="DequeueServiceConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DequeueServiceConfig.</returns>
        public static DequeueServiceConfig RandomDequeueServiceConfig(Random random) =>
            new DequeueServiceConfig(random.Next(202, 299), random.Next(302, 399));

        /// <summary>
        /// Generate random <see cref="DownloadServiceConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DownloadServiceConfig.</returns>
        public static DownloadServiceConfig RandomDownloadServiceConfig(Random random) =>
            new DownloadServiceConfig(random.Next(2, 99), random.Next(102, 199));

        /// <summary>
        /// Generate random <see cref="GatewayProcessorConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random GatewayProcessorConfig.</returns>
        public static GatewayProcessorConfig RandomGatewayProcessorConfig(Random random) =>
            new GatewayProcessorConfig(
                RandomServiceSettings(random),
                RandomProcessorSettings(random),
                RandomDequeueServiceConfig(random),
                RandomDownloadServiceConfig(random),
                RandomConfigurationServiceConfig(random));

        /// <summary>
        /// Generate random <see cref="DicomTagIndex"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DicomTagIndex.</returns>
        public static DicomTagIndex RandomDicomTagIndex(Random random) =>
            new DicomTagIndex(RandomUShort(random), RandomUShort(random));

        /// <summary>
        /// Generate random <see cref="DicomOrderedTag"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DicomOrderedTag<DateTime>.</returns>
        public static DicomOrderedTag<DateTime> RandomDicomOrderedTagDateTime(Random random) =>
            new DicomOrderedTag<DateTime>(
                RandomEnum<Order>(random),
                DateTime.UtcNow.AddDays(random.NextDouble() * 1000.0),
                random.Next(100));

        /// <summary>
        /// Generate random <see cref="DicomOrderedTag"/>.
        /// </summary>
        /// <remarks>Doubles are only created with two decimal places, because it is not expected that very
        /// high accuracy will not be used in actual configuration files, and the round-trip to and from JSON
        /// sometimes fails.</remarks>
        /// <param name="random">Random.</param>
        /// <returns>Random DicomOrderedTag<double>.</returns>
        public static DicomOrderedTag<double> RandomDicomOrderedTagDouble(Random random) =>
            new DicomOrderedTag<double>(
                RandomEnum<Order>(random),
                Math.Round(random.NextDouble() * 1000.0, 2),
                random.Next(100));

        /// <summary>
        /// Generate random <see cref="DicomOrderedTag"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DicomOrderedTag<int>.</returns>
        public static DicomOrderedTag<int> RandomDicomOrderedTagInt(Random random) =>
            new DicomOrderedTag<int>(
                RandomEnum<Order>(random),
                random.Next(101, 200),
                random.Next(100));

        /// <summary>
        /// Generate random <see cref="DicomOrderedTag"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DicomOrderedTag<OrderedString>.</returns>
        public static DicomOrderedTag<OrderedString> RandomDicomOrderedTagString(Random random) =>
            new DicomOrderedTag<OrderedString>(
                RandomEnum<Order>(random),
                new OrderedString(RandomString(random, 12)),
                random.Next(100));

        /// <summary>
        /// Generate random <see cref="DicomOrderedTag"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DicomOrderedTag<TimeSpan>.</returns>
        public static DicomOrderedTag<TimeSpan> RandomDicomOrderedTagTimeSpan(Random random) =>
            new DicomOrderedTag<TimeSpan>(
                RandomEnum<Order>(random),
                TimeSpan.FromMinutes(random.NextDouble() * 1000.0),
                random.Next(100));

        /// <summary>
        /// Generate random <see cref="GroupTagConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random GroupTagConstraint.</returns>
        public static GroupTagConstraint RandomGroupTagConstraint(Random random, int maxDepth) =>
            new GroupTagConstraint(
                RandomGroupConstraint(random, maxDepth - 1),
                RandomDicomTagIndex(random));

        /// <summary>
        /// Generate random <see cref="OrderedDateTimeConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random OrderedDateTimeConstraint.</returns>
        public static OrderedDateTimeConstraint RandomOrderedDateTimeConstraint(Random random, int maxDepth) =>
            new OrderedDateTimeConstraint(
                RandomDicomTagIndex(random),
                RandomDicomOrderedTagDateTime(random));

        /// <summary>
        /// Generate random <see cref="OrderedDoubleConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random OrderedDoubleConstraint.</returns>
        public static OrderedDoubleConstraint RandomOrderedDoubleConstraint(Random random, int maxDepth) =>
            new OrderedDoubleConstraint(
                RandomDicomTagIndex(random),
                RandomDicomOrderedTagDouble(random));

        /// <summary>
        /// Generate random <see cref="OrderedIntConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random OrderedIntConstraint.</returns>
        public static OrderedIntConstraint RandomOrderedIntConstraint(Random random, int maxDepth) =>
            new OrderedIntConstraint(
                RandomDicomTagIndex(random),
                RandomDicomOrderedTagInt(random));

        /// <summary>
        /// Generate random <see cref="OrderedStringConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random OrderedStringConstraint.</returns>
        public static OrderedStringConstraint RandomOrderedStringConstraint(Random random, int maxDepth) =>
            new OrderedStringConstraint(
                RandomDicomTagIndex(random),
                RandomDicomOrderedTagString(random));

        /// <summary>
        /// Generate random <see cref="RegexConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random RegexConstraint.</returns>
        public static RegexConstraint RandomRegexConstraint(Random random, int maxDepth) =>
            new RegexConstraint(
                RandomDicomTagIndex(random),
                RandomString(random, 18),
                RandomEnum<RegexOptions>(random),
                random.Next(100));

        /// <summary>
        /// Generate random <see cref="RequiredTagConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random RequiredTagConstraint.</returns>
        public static RequiredTagConstraint RandomRequiredTagConstraint(Random random, int maxDepth) =>
            new RequiredTagConstraint(
                RandomEnum<TagRequirement>(random),
                RandomDicomTagConstraint(random, maxDepth));

        /// <summary>
        /// Generate random <see cref="StringContainsConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random StringContainsConstraint.</returns>
        public static StringContainsConstraint RandomStringContainsConstraint(Random random, int maxDepth) =>
            new StringContainsConstraint(
                RandomDicomTagIndex(random),
                RandomString(random, 5),
                random.Next(100));

        /// <summary>
        /// Generate random <see cref="TimeOrderConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random TimeOrderConstraint.</returns>
        public static TimeOrderConstraint RandomTimeOrderConstraint(Random random, int maxDepth) =>
            new TimeOrderConstraint(
                RandomDicomTagIndex(random),
                RandomDicomOrderedTagTimeSpan(random));

        /// <summary>
        /// Generate random <see cref="UIDStringOrderConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random UIDStringOrderConstraint.</returns>
        public static UIDStringOrderConstraint RandomUIDStringOrderConstraint(Random random, int maxDepth) =>
            new UIDStringOrderConstraint(
                RandomDicomTagIndex(random),
                RandomDicomOrderedTagString(random));

        /// <summary>
        /// DicomConstraint generators, excluding RandomGroupTagConstraint.
        /// </summary>
        /// <remarks>
        /// Exclude RandomGroupTagConstraint to prevent stack overflow.
        /// </remarks>
        public static readonly Func<Random, int, DicomConstraint>[] RandomDicomConstraintGenerators =
        {
            RandomOrderedDateTimeConstraint,
            RandomOrderedDoubleConstraint,
            RandomOrderedIntConstraint,
            RandomOrderedStringConstraint,
            RandomRegexConstraint,
            RandomRequiredTagConstraint,
            RandomStringContainsConstraint,
            RandomTimeOrderConstraint,
            RandomUIDStringOrderConstraint,
        };

        /// <summary>
        /// DicomConstraint generators, including RandomGroupTagConstraint.
        /// </summary>
        public static readonly Func<Random, int, DicomConstraint>[] RandomDicomConstraintGeneratorsWithGroup =
            RandomDicomConstraintGenerators.Concat(new Func<Random, int, DicomConstraint>[] { RandomGroupTagConstraint }).ToArray();

        /// <summary>
        /// Generate random <see cref="DicomConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random DicomConstraint.</returns>
        public static DicomConstraint RandomDicomConstraint(Random random, int maxDepth) =>
            RandomItem(random, maxDepth, maxDepth > 0 ? RandomDicomConstraintGeneratorsWithGroup : RandomDicomConstraintGenerators);

        /// <summary>
        /// DicomTagConstraint generators, excluding RandomGroupTagConstraint.
        /// </summary>
        /// <remarks>
        /// Exclude RandomGroupTagConstraint to prevent stack overflow.
        /// </remarks>
        public static readonly Func<Random, int, DicomTagConstraint>[] RandomDicomTagConstraintGenerators =
        {
            RandomOrderedDateTimeConstraint,
            RandomOrderedDoubleConstraint,
            RandomOrderedIntConstraint,
            RandomOrderedStringConstraint,
            RandomRegexConstraint,
            RandomStringContainsConstraint,
            RandomTimeOrderConstraint,
            RandomUIDStringOrderConstraint,
        };

        /// <summary>
        /// DicomTagConstraint generators, including RandomGroupTagConstraint.
        /// </summary>
        public static readonly Func<Random, int, DicomTagConstraint>[] RandomDicomTagConstraintGeneratorsWithGroup =
            RandomDicomTagConstraintGenerators.Concat(new Func<Random, int, DicomTagConstraint>[] { RandomGroupTagConstraint }).ToArray();

        /// <summary>
        /// Generate random <see cref="DicomTagConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random DicomTagConstraint.</returns>
        public static DicomTagConstraint RandomDicomTagConstraint(Random random, int maxDepth) =>
            RandomItem(random, maxDepth, maxDepth > 0 ? RandomDicomTagConstraintGeneratorsWithGroup : RandomDicomTagConstraintGenerators);

        /// <summary>
        /// Generate a random array, possibly empty, of <see cref="DicomConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random array of DicomConstraints.</returns>
        public static DicomConstraint[] RandomDicomConstraints(Random random, int maxDepth) =>
            RandomBool(random) ? RandomArray(random, maxDepth, 20, RandomDicomConstraint) : Array.Empty<DicomConstraint>();

        /// <summary>
        /// Generate random <see cref="GroupConstraint"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random GroupConstraint.</returns>
        public static GroupConstraint RandomGroupConstraint(Random random, int maxDepth) =>
            new GroupConstraint(
                RandomDicomConstraints(random, maxDepth),
                RandomEnum<LogicalOperator>(random));

        /// <summary>
        /// Generate random <see cref="ModelChannelConstraints"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random ModelChannelConstraints.</returns>
        public static ModelChannelConstraints RandomModelChannelConstraint(Random random, int maxDepth) =>
            new ModelChannelConstraints(
                RandomString(random),
                RandomGroupConstraint(random, maxDepth),
                RandomGroupConstraint(random, maxDepth),
                random.Next(2, 5),
                random.Next(5, 9));

        /// <summary>
        /// Generate random <see cref="TagReplacement"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random TagReplacement.</returns>
        public static TagReplacement RandomTagReplacement(Random random, int maxDepth) =>
            new TagReplacement(
                RandomEnum<TagReplacementOperation>(random),
                RandomDicomTagIndex(random),
                RandomString(random));

        /// <summary>
        /// Generate random <see cref="ModelConstraintsConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random ModelConstraintsConfig.</returns>
        public static ModelConstraintsConfig RandomModelConstraintsConfig(Random random, int maxDepth) =>
            new ModelConstraintsConfig(
                RandomString(random, 9),
                RandomArray(random, maxDepth, 4, RandomModelChannelConstraint),
                RandomArray(random, maxDepth, 7, RandomTagReplacement));

        /// <summary>
        /// Generate random <see cref="AETConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random AETConfig.</returns>
        public static AETConfig RandomAETConfig(Random random, int maxDepth)
        {
            var aetConfigType = RandomEnum<AETConfigType>(random);
            var modelsConfig = AETConfig.NeedsModelConfig(aetConfigType) ? RandomArray(random, maxDepth, 5, RandomModelConstraintsConfig) : null;

            return new AETConfig(
                aetConfigType,
                modelsConfig);
        }

        /// <summary>
        /// Generate random <see cref="ClientAETConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random ClientAETConfig.</returns>
        public static ClientAETConfig RandomClientAETConfig(Random random, int maxDepth) =>
            new ClientAETConfig(
                RandomAETConfig(random, maxDepth),
                RandomDicomEndPoint(random),
                RandomBool(random));

        /// <summary>
        /// Generate random <see cref="AETConfigModel"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="maxDepth">Limit nesting on group tags.</param>
        /// <returns>Random AETConfigModel.</returns>
        public static AETConfigModel RandomAETConfigModel(Random random, int maxDepth) =>
            new AETConfigModel(
                RandomString(random, 10),
                RandomString(random, 11),
                RandomClientAETConfig(random, maxDepth));

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a random gateway receive config, saves it, and checks it loads correctly.")]
        [TestMethod]
        public void TestLoadGatewayReceiveConfig()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var expectedGatewayReceiveConfig = RandomGatewayReceiveConfig(random);
            Serialise(expectedGatewayReceiveConfig, configurationDirectory, GatewayReceiveConfigProvider.GatewayReceiveConfigFileName);

            var gatewayReceiveConfigProvider = new GatewayReceiveConfigProvider(_baseTestLogger, configurationDirectory);
            var actualGatewayReceiveConfig = gatewayReceiveConfigProvider.GatewayReceiveConfig();

            Assert.AreEqual(expectedGatewayReceiveConfig, actualGatewayReceiveConfig);
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a random gateway processor config, saves it, and checks it loads correctly.")]
        [TestMethod]
        public void TestLoadGatewayProcessorConfig()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var expectedGatewayProcessorConfig = RandomGatewayProcessorConfig(random);
            Serialise(expectedGatewayProcessorConfig, configurationDirectory, GatewayProcessorConfigProvider.GatewayProcessorConfigFileName);

            var gatewayProcessorConfigProvider = new GatewayProcessorConfigProvider(_baseTestLogger, configurationDirectory);
            var actualGatewayProcessorConfig = gatewayProcessorConfigProvider.GatewayProcessorConfig();

            Assert.AreEqual(expectedGatewayProcessorConfig, actualGatewayProcessorConfig);
        }

        /// <summary>
        /// Create a list of random AET config models, save them to a single file, and check they load correctly.
        /// </summary>
        /// <param name="useFile">True to use AETConfigProvider in single file mode, false to use it in folder mode.</param>
        public void TestLoadAETConfigCommon(bool useFile)
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var expectedAETConfigModels = RandomArray(random, 2, 10, RandomAETConfigModel);
            var folder = string.Empty;
            var filename = string.Empty;

            if (useFile)
            {
                folder = configurationDirectory;
                filename = AETConfigProvider.AETConfigFileName;
            }
            else
            {
                folder = Path.Combine(configurationDirectory, AETConfigProvider.AETConfigFolderName);
                Directory.CreateDirectory(folder);
                filename = "test1.json";
            }

            Serialise(expectedAETConfigModels, folder, filename);

            var aetConfigProvider = new AETConfigProvider(_baseTestLogger, configurationDirectory, useFile);
            var actualAETConfigModels = aetConfigProvider.GetAETConfigs().ToArray();

            Assert.IsTrue(expectedAETConfigModels.SequenceEqual(actualAETConfigModels));
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a list of AET config models, saves it to a file, and checks it loads correctly.")]
        [TestMethod]
        public void TestLoadAETConfigFile()
        {
            TestLoadAETConfigCommon(true);
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a list of AET config models, saves it to a file in a folder, and checks it loads correctly.")]
        [TestMethod]
        public void TestLoadAETConfigFolder()
        {
            TestLoadAETConfigCommon(false);
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a list of AET config models, saves them along with two other config files, and checks the models load correctly" +
            "and the other configs are ignored.")]
        [TestMethod]
        public void TestLoadAETConfigInvalidFiles()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var aetConfigFolder = Path.Combine(configurationDirectory, AETConfigProvider.AETConfigFolderName);
            Directory.CreateDirectory(aetConfigFolder);

            var expectedAETConfigModels = RandomArray(random, 3, 10, RandomAETConfigModel);
            Serialise(expectedAETConfigModels, aetConfigFolder, "test1.json");

            // Write a random GatewayProcessorConfig
            var gatewayProcessorConfig = RandomGatewayProcessorConfig(random);
            Serialise(gatewayProcessorConfig, aetConfigFolder, "test2.json");

            // Write a random GatewayReceiverConfig
            var gatewayReceiveConfig = RandomGatewayReceiveConfig(random);
            Serialise(gatewayReceiveConfig, aetConfigFolder, "test3.json");

            var aetConfigProvider = new AETConfigProvider(_baseTestLogger, configurationDirectory);
            var actualAETConfigModels = aetConfigProvider.GetAETConfigs().ToArray();

            Assert.IsTrue(expectedAETConfigModels.SequenceEqual(actualAETConfigModels));
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a list of AET config models, saves them to one file per called/calling pair, and checks they all load correctly.")]
        [TestMethod]
        public void TestLoadAETConfigConcatenate()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var aetConfigFolder = Path.Combine(configurationDirectory, AETConfigProvider.AETConfigFolderName);
            Directory.CreateDirectory(aetConfigFolder);

            var expectedAETConfigModels = RandomArray(random, 3, 10, RandomAETConfigModel);
            for (var i = 0; i < expectedAETConfigModels.Length; i++)
            {
                var expectedAETConfig = new[] { expectedAETConfigModels[i] };
                Serialise(expectedAETConfig, aetConfigFolder, string.Format("GatewayModelRulesConfig{0}.json", i));
            }

            var aetConfigProvider = new AETConfigProvider(_baseTestLogger, configurationDirectory);
            var actualAETConfigModels = aetConfigProvider.GetAETConfigs().ToArray();

            Assert.IsTrue(expectedAETConfigModels.SequenceEqual(actualAETConfigModels));
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a single AET config model, splits it at the model config point, saves each to a file, and checks they all load correctly.")]
        [TestMethod]
        public void TestLoadAETConfigMerge()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var aetConfigFolder = Path.Combine(configurationDirectory, AETConfigProvider.AETConfigFolderName);
            Directory.CreateDirectory(aetConfigFolder);

            // This test needs ModelConfigs, but the AETConfigType will be one of: Model, ModelDryRun, ModelWithResultDryRun
            // just loop until it has the right type.
            var expectedAETConfigModels = RandomArray(random, 3, 1, RandomAETConfigModel);
            while (expectedAETConfigModels[0].AETConfig.Config.ModelsConfig == null ||
                expectedAETConfigModels[0].AETConfig.Config.ModelsConfig.Length == 0)
            {
                expectedAETConfigModels = RandomArray(random, 3, 1, RandomAETConfigModel);
            }

            for (var i = 0; i < expectedAETConfigModels[0].AETConfig.Config.ModelsConfig.Length; i++)
            {
                // Clone the expected AET config model taking only the ith models config.
                var expectedAETConfig0 = expectedAETConfigModels[0].With(
                    aetConfig: expectedAETConfigModels[0].AETConfig.With(
                        config: expectedAETConfigModels[0].AETConfig.Config.With(
                            modelsConfig: new[] { expectedAETConfigModels[0].AETConfig.Config.ModelsConfig[i] })));

                var expectedAETConfig = new[] { expectedAETConfig0 };
                Serialise(expectedAETConfig, aetConfigFolder, string.Format("GatewayModelRulesConfig{0}.json", i), true);
            }

            var aetConfigProvider = new AETConfigProvider(_baseTestLogger, configurationDirectory);
            var actualAETConfigModels = aetConfigProvider.GetAETConfigs().ToArray();

            Assert.IsTrue(expectedAETConfigModels.SequenceEqual(actualAETConfigModels));
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates an AET config, splits it the base and at the model config point, saves each into a separate file, and check they all load correctly.")]
        [TestMethod]
        public void TestLoadAETConfigSplitAndMerge()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var aetConfigFolder = Path.Combine(configurationDirectory, AETConfigProvider.AETConfigFolderName);
            Directory.CreateDirectory(aetConfigFolder);

            var expectedAETConfigModels = RandomArray(random, 3, 10, RandomAETConfigModel);

            for (var j = 0; j < expectedAETConfigModels.Length; j++)
            {
                if (expectedAETConfigModels[j].AETConfig.Config.ModelsConfig != null)
                {
                    // If this model has ModelsConfig then create a new AET config model for each of the 
                    // models config.
                    for (var i = 0; i < expectedAETConfigModels[j].AETConfig.Config.ModelsConfig.Length; i++)
                    {
                        // Clone the expected AET config model taking only the ith models config.
                        var expectedAETConfig0 = expectedAETConfigModels[j].With(
                            aetConfig: expectedAETConfigModels[j].AETConfig.With(
                                config: expectedAETConfigModels[j].AETConfig.Config.With(
                                    modelsConfig: new[] { expectedAETConfigModels[j].AETConfig.Config.ModelsConfig[i] })));

                        var expectedAETConfig = new[] { expectedAETConfig0 };
                        Serialise(expectedAETConfig, aetConfigFolder, string.Format("GatewayModelRulesConfig{0}_{1}.json", j, i), true);
                    }
                }
                else
                {
                    // No ModelsConfig so just clone at the base.
                    var expectedAETConfig = new[] { expectedAETConfigModels[j] };
                    Serialise(expectedAETConfig, aetConfigFolder, string.Format("GatewayModelRulesConfig{0}.json", j), true);
                }
            }

            var aetConfigProvider = new AETConfigProvider(_baseTestLogger, configurationDirectory);
            var actualAETConfigModels = aetConfigProvider.GetAETConfigs().ToArray();

            Assert.IsTrue(expectedAETConfigModels.SequenceEqual(actualAETConfigModels));
        }
    }
}
