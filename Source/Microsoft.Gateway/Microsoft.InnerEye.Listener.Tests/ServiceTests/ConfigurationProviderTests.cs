namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
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
        public static ConfigurationServiceConfig RandomConfigurationServiceConfig(Random random)
        {
            random = random ?? throw new ArgumentNullException(nameof(random));

            return new ConfigurationServiceConfig(
                DateTime.UtcNow.AddSeconds(5),
                DateTime.UtcNow.AddSeconds(10),
                random.Next(61, 3600));
        }

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
        public static DequeueServiceConfig RandomDequeueServiceConfig(Random random)
        {
            random = random ?? throw new ArgumentNullException(nameof(random));

            return new DequeueServiceConfig(random.Next(202, 299), random.Next(302, 399));
        }

        /// <summary>
        /// Generate random <see cref="DownloadServiceConfig"/>.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <returns>Random DownloadServiceConfig.</returns>
        public static DownloadServiceConfig RandomDownloadServiceConfig(Random random)
        {
            random = random ?? throw new ArgumentNullException(nameof(random));

            return new DownloadServiceConfig(random.Next(2, 99), random.Next(102, 199));
        }

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
        [DataRow(false, DisplayName = "Load GatewayReceiveConfig")]
        [DataRow(true, DisplayName = "Reload GatewayReceiveConfig")]
        [TestMethod]
        public void TestLoadGatewayReceiveConfig(bool reload)
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var expectedGatewayReceiveConfig = RandomGatewayReceiveConfig(random);
            Serialise(expectedGatewayReceiveConfig, configurationDirectory, GatewayReceiveConfigProvider.GatewayReceiveConfigFileName);

            using (var gatewayReceiveConfigProvider = CreateGatewayReceiveConfigProvider(configurationDirectory))
            {
                Assert.AreEqual(expectedGatewayReceiveConfig, gatewayReceiveConfigProvider.Config);

                if (reload)
                {
                    var configReloadedCount = 0;

                    gatewayReceiveConfigProvider.ConfigChanged += (s, e) =>
                    {
                        Interlocked.Increment(ref configReloadedCount);
                    };

                    var expectedGatewayReceiveConfig2 = RandomGatewayReceiveConfig(random);
                    Serialise(expectedGatewayReceiveConfig2, configurationDirectory, GatewayReceiveConfigProvider.GatewayReceiveConfigFileName);

                    SpinWait.SpinUntil(() => configReloadedCount > 0, TimeSpan.FromSeconds(10));

                    Assert.AreEqual(expectedGatewayReceiveConfig2, gatewayReceiveConfigProvider.Config);
                }
            }
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a random gateway receive config, saves it, and checks it loads correctly. Then toggles runAsConsole.")]
        [TestMethod]
        public void TestUpdateGatewayReceiveConfigRunAsConsole()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var expectedGatewayReceiveConfig = RandomGatewayReceiveConfig(random);
            Serialise(expectedGatewayReceiveConfig, configurationDirectory, GatewayReceiveConfigProvider.GatewayReceiveConfigFileName);

            using (var gatewayReceiveConfigProvider = CreateGatewayReceiveConfigProvider(configurationDirectory))
            {
                Assert.AreEqual(expectedGatewayReceiveConfig, gatewayReceiveConfigProvider.Config);

                var configReloadedCount = 0;

                gatewayReceiveConfigProvider.ConfigChanged += (s, e) =>
                {
                    Interlocked.Increment(ref configReloadedCount);
                };

                var runAsConsole = gatewayReceiveConfigProvider.Config.ServiceSettings.RunAsConsole;

                gatewayReceiveConfigProvider.SetRunAsConsole(!runAsConsole);

                SpinWait.SpinUntil(() => configReloadedCount > 0, TimeSpan.FromSeconds(10));

                // RunAsConsole should have now toggled.
                Assert.AreEqual(!runAsConsole, gatewayReceiveConfigProvider.Config.ServiceSettings.RunAsConsole);
            }
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a random gateway processor config, saves it, and checks it loads correctly.")]
        [DataRow(false, DisplayName = "Load GatewayProcessorConfig")]
        [DataRow(true, DisplayName = "Reload GatewayProcessorConfig")]
        [TestMethod]
        public void TestLoadGatewayProcessorConfig(bool reload)
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var expectedGatewayProcessorConfig = RandomGatewayProcessorConfig(random);
            Serialise(expectedGatewayProcessorConfig, configurationDirectory, GatewayProcessorConfigProvider.GatewayProcessorConfigFileName);

            using (var gatewayProcessorConfigProvider = CreateGatewayProcessorConfigProvider(configurationDirectory))
            {
                Assert.AreEqual(expectedGatewayProcessorConfig, gatewayProcessorConfigProvider.Config);

                if (reload)
                {
                    var configReloadedCount = 0;

                    gatewayProcessorConfigProvider.ConfigChanged += (s, e) =>
                    {
                        Interlocked.Increment(ref configReloadedCount);
                    };

                    var expectedGatewayProcessorConfig2 = RandomGatewayProcessorConfig(random);
                    Serialise(expectedGatewayProcessorConfig2, configurationDirectory, GatewayProcessorConfigProvider.GatewayProcessorConfigFileName);

                    SpinWait.SpinUntil(() => configReloadedCount > 0, TimeSpan.FromSeconds(10));

                    Assert.AreEqual(expectedGatewayProcessorConfig2, gatewayProcessorConfigProvider.Config);
                }
            }
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a random gateway processor config, saves it, and checks it loads correctly. Then toggles runAsConsole.")]
        [TestMethod]
        public void TestUpdateGatewayProcessorConfigRunAsConsole()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var expectedGatewayProcessorConfig = RandomGatewayProcessorConfig(random);
            Serialise(expectedGatewayProcessorConfig, configurationDirectory, GatewayProcessorConfigProvider.GatewayProcessorConfigFileName);

            using (var gatewayProcessorConfigProvider = CreateGatewayProcessorConfigProvider(configurationDirectory))
            {
                Assert.AreEqual(expectedGatewayProcessorConfig, gatewayProcessorConfigProvider.Config);

                var configReloadedCount = 0;

                gatewayProcessorConfigProvider.ConfigChanged += (s, e) =>
                {
                    Interlocked.Increment(ref configReloadedCount);
                };

                var runAsConsole = gatewayProcessorConfigProvider.Config.ServiceSettings.RunAsConsole;

                gatewayProcessorConfigProvider.SetRunAsConsole(!runAsConsole);

                SpinWait.SpinUntil(() => configReloadedCount > 0, TimeSpan.FromSeconds(10));

                // RunAsConsole should have now toggled.
                Assert.AreEqual(!runAsConsole, gatewayProcessorConfigProvider.Config.ServiceSettings.RunAsConsole);
            }
        }

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a random gateway processor config, saves it, and checks it loads correctly. Then toggles updates processor settings.")]
        [TestMethod]
        public void TestUpdateGatewayProcessorConfigProcessorSettings()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var expectedGatewayProcessorConfig = RandomGatewayProcessorConfig(random);
            Serialise(expectedGatewayProcessorConfig, configurationDirectory, GatewayProcessorConfigProvider.GatewayProcessorConfigFileName);

            using (var gatewayProcessorConfigProvider = CreateGatewayProcessorConfigProvider(configurationDirectory))
            {
                Assert.AreEqual(expectedGatewayProcessorConfig, gatewayProcessorConfigProvider.Config);

                var configReloadedCount = 0;

                gatewayProcessorConfigProvider.ConfigChanged += (s, e) =>
                {
                    Interlocked.Increment(ref configReloadedCount);
                };

                var processorSettings = RandomProcessorSettings(random);

                gatewayProcessorConfigProvider.SetProcessorSettings(processorSettings.InferenceUri);

                SpinWait.SpinUntil(() => configReloadedCount > 0, TimeSpan.FromSeconds(10));

                // InferenceUri should have now changed.
                Assert.AreEqual(processorSettings.InferenceUri, gatewayProcessorConfigProvider.Config.ProcessorSettings.InferenceUri);
            }
        }

        /// <summary>
        /// Create an Action for saving a set of AETConfigModels to a single file.
        /// </summary>
        /// <param name="configurationDirectory">Folder to store file in.</param>
        /// <returns>Action.</returns>
        public static Action<Random, AETConfigModel[]> SaveFileAETConfigModels(string configurationDirectory) =>
            (random, aetConfigModels) => Serialise(aetConfigModels, configurationDirectory, AETConfigProvider.AETConfigFileName);

        /// <summary>
        /// Create an Action for saving a set of AETConfigModels to a folder.
        /// </summary>
        /// <param name="configurationDirectory">Folder to store folder in.</param>
        /// <param name="addJunk">True to add junk files that should be ignored.</param>
        /// <param name="multipleFiles">True to split config across multiple files.</param>
        /// <returns>Action.</returns>
        public static Action<Random, AETConfigModel[]> SaveFolderAETConfigModels(string configurationDirectory, bool addJunk, bool multipleFiles) =>
            (random, aetConfigModels) =>
            {
                var folder = Path.Combine(configurationDirectory, AETConfigProvider.AETConfigFolderName);
                Directory.CreateDirectory(folder);

                // Add in two extra files that should be ignored.
                if (addJunk)
                {
                    // Write a random GatewayProcessorConfig
                    var gatewayProcessorConfig = RandomGatewayProcessorConfig(random);
                    Serialise(gatewayProcessorConfig, folder, GatewayProcessorConfigProvider.GatewayProcessorConfigFileName);

                    // Write a random GatewayReceiverConfig
                    var gatewayReceiveConfig = RandomGatewayReceiveConfig(random);
                    Serialise(gatewayReceiveConfig, folder, GatewayReceiveConfigProvider.GatewayReceiveConfigFileName);
                }

                if (multipleFiles)
                {
                    for (var i = 0; i < aetConfigModels.Length; i++)
                    {
                        var expectedAETConfig = new[] { aetConfigModels[i] };
                        Serialise(expectedAETConfig, folder, string.Format(CultureInfo.InvariantCulture, "test{0}.json", i + 1));
                    }
                }
                else
                {
                    Serialise(aetConfigModels, folder, "test1.json");
                }
            };

        public static AETConfigModel[] OrderAETConfigModels(IEnumerable<AETConfigModel> aetConfigModels) =>
            aetConfigModels.OrderBy(m => m.CalledAET).ThenBy(m => m.CallingAET).ToArray();

        public static void AssertAETConfigModelsEqual(IEnumerable<AETConfigModel> expectedAETConfigModels, IEnumerable<AETConfigModel> actualAETConfigModels) =>
            Assert.IsTrue(OrderAETConfigModels(expectedAETConfigModels).SequenceEqual(OrderAETConfigModels(actualAETConfigModels)));

        [TestCategory("ConfigurationProvider")]
        [Description("Creates a list of AET config models, saves it to a file or folder, and checks it loads correctly.")]
        [DataRow(false, false, false, false, DisplayName = "Load folder AETConfigModels")]
        [DataRow(false, false, false, true, DisplayName = "Load folder AETConfigModels, split files")]
        [DataRow(false, false, true, false, DisplayName = "Load folder AETConfigModels, ignore junk")]
        [DataRow(false, true, false, false, DisplayName = "Reload folder AETConfigModels")]
        [DataRow(false, true, true, false, DisplayName = "Reload folder AETConfigModels, ignore junk")]
        [DataRow(true, false, false, false, DisplayName = "Load file AETConfigModels")]
        [DataRow(true, true, false, false, DisplayName = "Reload file AETConfigModels")]
        [TestMethod]
        public void TestLoadAETConfigModels(bool useFile, bool reload, bool addJunk, bool multipleFiles)
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;
            var random = new Random();

            var saveAETConfigModels = useFile ?
                SaveFileAETConfigModels(configurationDirectory) :
                SaveFolderAETConfigModels(configurationDirectory, addJunk, multipleFiles);

            var expectedAETConfigModels = RandomArray(random, 3, 10, RandomAETConfigModel);
            saveAETConfigModels.Invoke(random, expectedAETConfigModels);

            using (var aetConfigProvider = CreateAETConfigProvider(configurationDirectory, useFile))
            {
                AssertAETConfigModelsEqual(expectedAETConfigModels, aetConfigProvider.Config);

                if (reload)
                {
                    var configReloadedCount = 0;

                    aetConfigProvider.ConfigChanged += (s, e) =>
                    {
                        Interlocked.Increment(ref configReloadedCount);
                    };

                    var expectedAETConfigModels2 = RandomArray(random, 3, 10, RandomAETConfigModel);
                    saveAETConfigModels.Invoke(random, expectedAETConfigModels2);

                    SpinWait.SpinUntil(() => configReloadedCount > (addJunk ? 4 : 0), TimeSpan.FromSeconds(10));

                    AssertAETConfigModelsEqual(expectedAETConfigModels2, aetConfigProvider.Config);
                }
            }
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
                expectedAETConfigModels[0].AETConfig.Config.ModelsConfig.Count == 0)
            {
                expectedAETConfigModels = RandomArray(random, 3, 1, RandomAETConfigModel);
            }

            for (var i = 0; i < expectedAETConfigModels[0].AETConfig.Config.ModelsConfig.Count; i++)
            {
                // Clone the expected AET config model taking only the ith models config.
                var expectedAETConfig0 = expectedAETConfigModels[0].With(
                    aetConfig: expectedAETConfigModels[0].AETConfig.With(
                        config: expectedAETConfigModels[0].AETConfig.Config.With(
                            modelsConfig: new[] { expectedAETConfigModels[0].AETConfig.Config.ModelsConfig[i] })));

                var expectedAETConfig = new[] { expectedAETConfig0 };
                Serialise(expectedAETConfig, aetConfigFolder, string.Format(CultureInfo.InvariantCulture, "GatewayModelRulesConfig{0}.json", i), true);
            }

            using (var aetConfigProvider = CreateAETConfigProvider(configurationDirectory))
            {
                AssertAETConfigModelsEqual(expectedAETConfigModels, aetConfigProvider.Config);
            }
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
                    for (var i = 0; i < expectedAETConfigModels[j].AETConfig.Config.ModelsConfig.Count; i++)
                    {
                        // Clone the expected AET config model taking only the ith models config.
                        var expectedAETConfig0 = expectedAETConfigModels[j].With(
                            aetConfig: expectedAETConfigModels[j].AETConfig.With(
                                config: expectedAETConfigModels[j].AETConfig.Config.With(
                                    modelsConfig: new[] { expectedAETConfigModels[j].AETConfig.Config.ModelsConfig[i] })));

                        var expectedAETConfig = new[] { expectedAETConfig0 };
                        Serialise(expectedAETConfig, aetConfigFolder, string.Format(CultureInfo.InvariantCulture, "GatewayModelRulesConfig{0}_{1}.json", j, i), true);
                    }
                }
                else
                {
                    // No ModelsConfig so just clone at the base.
                    var expectedAETConfig = new[] { expectedAETConfigModels[j] };
                    Serialise(expectedAETConfig, aetConfigFolder, string.Format(CultureInfo.InvariantCulture, "GatewayModelRulesConfig{0}.json", j), true);
                }
            }

            using (var aetConfigProvider = CreateAETConfigProvider(configurationDirectory))
            {
                AssertAETConfigModelsEqual(expectedAETConfigModels, aetConfigProvider.Config);
            }
        }
    }
}
