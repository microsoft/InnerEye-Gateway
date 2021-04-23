﻿namespace Microsoft.InnerEye.Listener.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom;
    using Markdig;
    using Microsoft.Extensions.Logging;
    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
    using Microsoft.InnerEye.Gateway.MessageQueueing;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.InnerEye.Listener.Common.Services;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.Processor.Services;
    using Microsoft.InnerEye.Listener.Receiver.Services;
    using Microsoft.InnerEye.Listener.Tests.Models;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TheArtOfDev.HtmlRenderer.PdfSharp;

    /// <summary>
    /// The base test class.
    /// </summary>
    [TestClass]
    public class BaseTestClass
    {
        /// <summary>
        /// LoggerFactory for creating more ILoggers.
        /// </summary>
        protected readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;

        /// <summary>
        /// Logger for common use.
        /// </summary>
        protected readonly ILogger _baseTestLogger;

        /// <summary>
        /// Gets or sets the test context.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Gets a test queue path.
        /// </summary>
        private const string TestQueuePath = @".\private$\TestQueue1";

        /// <summary>
        /// Gets the test upload queue path.
        /// </summary>
        private const string TestUploadQueuePath = @".\private$\ListenerTestUpload";

        /// <summary>
        /// Gets the test download queue path.
        /// </summary>
        private const string TestDownloadQueuePath = @".\private$\ListenerTestDownload";

        /// <summary>
        /// Gets the test push queue path.
        /// </summary>
        private const string TestPushQueuePath = @".\private$\ListenerTestPush";

        /// <summary>
        /// The test delete queue path
        /// </summary>
        private const string TestDeleteQueuePath = @".\private$\DeleteTest";

        /// <summary>
        /// Create a new, random, message queue path.
        /// </summary>
        /// <returns></returns>
        private static string GetUniqueMessageQueuePath() => $@".\Private$\{Guid.NewGuid()}";

        /// <summary>
        /// Folder containing test configurations.
        /// </summary>
        private readonly string _basePathConfigs = "TestConfigurations";

        /// <summary>
        /// The temporary directories created during a test. The clean up method will also clean these up.
        /// </summary>
        private readonly IList<string> temporaryDirectories = new List<string>();

        /// <summary>
        /// AET configs as loaded from _basePathConfigs.
        /// </summary>
        private AETConfigProvider _testAETConfigProvider;

        /// <summary>
        /// GatewayProcessorConfigProvider as loaded from _basePathConfigs.
        /// </summary>
        protected GatewayProcessorConfigProvider TestGatewayProcessorConfigProvider { get; }

        /// <summary>
        /// GatewayReceiveConfigProvider as loaded from _basePathConfigs.
        /// </summary>
        private GatewayReceiveConfigProvider _testGatewayReceiveConfigProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTestClass"/> class.
        /// </summary>
        public BaseTestClass()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _baseTestLogger = _loggerFactory.CreateLogger("BaseTest");

            // Set a logger for fo-dicom network operations so that they show up in VS output when debugging
            Dicom.Log.LogManager.SetImplementation(new Dicom.Log.TextWriterLogManager(new DataProviderTests.DebugTextWriter()));

            _testAETConfigProvider = new AETConfigProvider(_loggerFactory.CreateLogger("ModelSettings"), _basePathConfigs);
            TestGatewayProcessorConfigProvider = new GatewayProcessorConfigProvider(_loggerFactory.CreateLogger("ProcessorSettings"), _basePathConfigs);
            _testGatewayReceiveConfigProvider = new GatewayReceiveConfigProvider(_loggerFactory.CreateLogger("ProcessorSettings"), _basePathConfigs);
        }

        [TestInitialize]
        public virtual void TestSetup()
        {
            TryDeleteDirectory(Path.Combine(Path.GetTempPath(), "InnerEyeListenerTestsTemp"));

            TryKillAnyZombieProcesses();

            ClearQueues(TestQueuePath, TestPushQueuePath, TestDownloadQueuePath, TestUploadQueuePath, TestDeleteQueuePath);
        }

        [TestCleanup]
        public virtual void TestCleanUp()
        {
            // Uncomment if redistribution of anonymization protocol or DCMTK tools are needed
            /*
            var (contents, extension) = ConvertMarkdownToPdf(CreateAnonymisationProtocol());
            var (readmeContents, readmeExtension) = ConvertMarkdownToPdf(File.ReadAllText(@"Assets\README.md"));

            // Write Store-SCU and DCMDump to the package folder
            WriteFileForBuildPackage(@"Assets\storescu.exe", "DicomTools");
            WriteFileForBuildPackage(@"Assets\dcmdump.exe", "DicomTools");
            WriteFileForBuildPackage(@"Assets\DCMTKLicense.txt", "DicomTools");
            WriteFileForBuildPackage($@"Assets\DataIngestionAnonymisationProtocol.pdf", "AnonymisationProtocols");
            WriteForBuildPackage($"Readme{readmeExtension}", string.Empty, readmeContents);
            WriteForBuildPackage($"SegmentationServiceAnonymisationProtocol{extension}", "AnonymisationProtocols", contents);

            // Convert all output MD files to HTML for easier reading
            ConvertAllOutputMarkdownFilesToHtml();
            */

            foreach (var directory in temporaryDirectories)
            {
                TryDeleteDirectory(directory);
            }

            TryDeleteDirectory(Path.Combine(Path.GetTempPath(), "InnerEyeListenerTestsTemp"));

            TryKillAnyZombieProcesses();
        }

        protected void WriteDicomFileForBuildPackage(string fileName, DicomFile dicomFile)
        {
            var path = GetBuildPackageResultPath(fileName, "AnonymisationProtocols");

            dicomFile.Save(path);

            TestContext.AddResultFile(path);
            TestContext.WriteLine($"Written Dicom file to path: {path}");
        }

        protected DequeueServiceConfig GetTestDequeueServiceConfig(
            uint maximumQueueMessageAgeSeconds = 100,
            uint deadLetterMoveFrequencySeconds = 1) =>
                new DequeueServiceConfig(maximumQueueMessageAgeSeconds, deadLetterMoveFrequencySeconds);

        /// <summary>
        /// Converts all markdown files in the output directory to HTML.
        /// This is easier to read out of the box than the markdown files.
        /// </summary>
        protected void ConvertAllOutputMarkdownFilesToHtml()
        {
            var directory = new DirectoryInfo(GetBuildPackageResultPath(string.Empty, string.Empty));

            if (!directory.Exists)
            {
                return;
            }

            var markdownFiles = directory.GetFiles("*.*", SearchOption.AllDirectories).Where(x => x.Extension == ".md");

            foreach (var markdownFile in markdownFiles)
            {
                try
                {
                    var (contents, extension) = ConvertMarkdownToPdf(File.ReadAllText(markdownFile.FullName));

                    var resultPath = Path.ChangeExtension(markdownFile.FullName, extension);

                    // Convert markdown file to html and replace extension.
                    File.WriteAllBytes(resultPath, contents);
                    TestContext.AddResultFile(resultPath);
                }
                catch (Exception e)
                {
                    _baseTestLogger.LogError(e, $"Failed to convert file to HTML. File {markdownFile}");
                }
            }
        }

        /// <summary>
        /// Converts markdown.
        /// </summary>
        /// <param name="markdown">The markdown to convert.</param>
        /// <returns>The contents and the resulting file extension.</returns>
        private static (byte[] Contents, string Extension) ConvertMarkdownToPdf(string markdown)
        {
            // Convert markdown file to html
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var html = Markdown.ToHtml(markdown, pipeline).Replace("<table>", "<table border=\"1\" rules=\"rows\">");

            // Convert HTML to pdf
            using (var memoryStream = new MemoryStream())
            {
                var pdf = PdfGenerator.GeneratePdf(html, PdfSharp.PageSize.A4);
                pdf.Save(memoryStream);

                return (memoryStream.ToArray(), ".pdf");
            }
        }

        protected static string GetBuildPackageResultPath(string fileName, string subDirectory = null)
        {
            var buildSourceDirectory = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");

            if (string.IsNullOrWhiteSpace(buildSourceDirectory))
            {
                buildSourceDirectory = @"C:";
            }

            var directory = $@"{buildSourceDirectory}\TestResultFiles\{subDirectory}";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return $@"{directory}\{fileName}";
        }

        /// <summary>
        /// Clears all the queues and its corresponding dead letter queue.
        /// </summary>
        /// <param name="queuePaths">Queue paths.</param>
        private static void ClearQueues(params string[] queuePaths)
        {
            foreach (var path in queuePaths)
            {
                // Note that this must be the same as DequeueClientServiceBase.DeadLetterQueuePathFormat.
                using (var deadLetterQueue = GatewayMessageQueue.Get(DequeueServiceConfig.DeadLetterQueuePath(path)))
                using (var queue = GatewayMessageQueue.Get(path))
                {
                    deadLetterQueue.Clear();
                    queue.Clear();
                }
            }
        }

        /// <summary>
        /// Get a test message queue.
        /// </summary>
        /// <returns>Test IMessageQueue.</returns>
        protected static IMessageQueue GetTestMessageQueue() => GatewayMessageQueue.Get(TestQueuePath);

        /// <summary>
        /// Get a unique test message queue.
        /// </summary>
        /// <returns>Unique test IMessageQueue.</returns>
        protected static IMessageQueue GetUniqueMessageQueue() => GatewayMessageQueue.Get(GetUniqueMessageQueuePath());

        protected DirectoryInfo CreateTemporaryDirectory()
        {
            var result = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $@"InnerEyeListenerTestsTemp\{Guid.NewGuid().ToString()}"));
            temporaryDirectories.Add(result.FullName);

            return result;
        }

        protected IEnumerable<DicomTagAnonymisation> SegmentationAnonymisationProtocol()
        {
            using (var segmentationClient = TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient().Invoke())
            {
                return segmentationClient.SegmentationAnonymisationProtocol;
            }
        }

        protected MockInnerEyeSegmentationClient GetMockInnerEyeSegmentationClient()
        {
            var realClient = (InnerEyeSegmentationClient)TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient().Invoke();
            return new MockInnerEyeSegmentationClient(realClient);
        }

        protected AETConfigModel GetTestAETConfigModel() =>
            _testAETConfigProvider.GetAETConfigs().First();

        /// <summary>
        /// Create ReceiveServiceConfig from test files, but overwrite the port and rootDicomFolder.
        /// </summary>
        /// <param name="port">New port.</param>
        /// <param name="rootDicomFolder">Optional folder, or will default to a temporary one.</param>
        /// <returns>New ReceiveServiceConfig.</returns>
        protected ReceiveServiceConfig GetTestGatewayReceiveServiceConfig(
            int port,
            DirectoryInfo rootDicomFolder = null)
        {
            var gatewayConfig = _testGatewayReceiveConfigProvider.GatewayReceiveConfig().ReceiveServiceConfig;

            return gatewayConfig.With(
                new DicomEndPoint(gatewayConfig.GatewayDicomEndPoint.Title, port, gatewayConfig.GatewayDicomEndPoint.Ip),
                (rootDicomFolder ?? CreateTemporaryDirectory()).FullName);
        }

        protected static void TransactionalEnqueue<T>(IMessageQueue InnerEyeMessageQueue, T value)
        {
            using (var queueTransaction = InnerEyeMessageQueue.CreateQueueTransaction())
            {
                try
                {
                    queueTransaction.Begin();
                    InnerEyeMessageQueue.Enqueue(value, queueTransaction);
                    queueTransaction.Commit();
                }
                catch (Exception)
                {
                    queueTransaction.Abort();
                    throw;
                }
            }
        }

        protected static T TransactionalDequeue<T>(IMessageQueue messageQueue, int timeoutMs = 2000)
        {
            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                using (var queueTransaction = messageQueue.CreateQueueTransaction())
                {
                    queueTransaction.Begin();
                    var result = TryDequeue<T>(messageQueue, queueTransaction, timeoutMs);
                    queueTransaction.Commit();

                    return result;
                }
            }

            throw new MessageQueueReadException("Failed to transactional dequeue.");
        }

        protected static T TryDequeue<T>(IMessageQueue messageQueue, IQueueTransaction messageQueueTransaction, int timeoutMs = 2000)
        {
            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    return messageQueue.DequeueNextMessage<T>(messageQueueTransaction);
                }
                catch (Exception)
                {
                    Task.WaitAll(Task.Delay(500));
                }
            }

            throw new MessageQueueReadException("Failed to transactional dequeue.");
        }

        protected void TryDeleteDirectory(string directory)
        {
            var directoryInfo = new DirectoryInfo(directory);

            if (directoryInfo.Exists)
            {
                try
                {
                    directoryInfo.Delete(true);
                }
                catch (Exception e)
                {
                    TestContext.WriteLine($"Failed to delete directory {directory} with exception {e}");
                }
            }
        }

        /// <summary>
        /// Start Dicom data receiver on a given port and check it is listening.
        /// </summary>
        /// <param name="dicomDataReceiver">Dicom data receiver.</param>
        /// <param name="port">Port.</param>
        protected void StartDicomDataReceiver(
            ListenerDataReceiver dicomDataReceiver,
            int port)
        {
            var started = dicomDataReceiver.StartServer(port, BuildAcceptedSopClassesAndTransferSyntaxes, TimeSpan.FromSeconds(2));
            Assert.IsTrue(started);
            Assert.IsTrue(dicomDataReceiver.IsListening);
        }

        /// <summary>
        /// Constructs the set of DICOM services we support in InnerEye and the preferred Transfer Syntaxes
        /// for those services. 
        /// </summary>
        /// <returns></returns>
        private static Dictionary<DicomUID, DicomTransferSyntax[]> BuildAcceptedSopClassesAndTransferSyntaxes()
        {
            // Syntaxes we accept for the Verification SOP (aka C-Echo) class in order of preference
            DicomTransferSyntax[] acceptedVerificationSyntaxes =
            {
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian
            };

            // For RT Storage, we accept the following in order of preference
            DicomTransferSyntax[] acceptedRTransferSyntaxs =
            {
                DicomTransferSyntax.ImplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian
            };

            // For CT and MR, we accept the following in order of preference
            DicomTransferSyntax[] acceptedImageTransferSyntaxs =
            {
                // Uncompressed
                DicomTransferSyntax.ImplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,

                // Lossless
                DicomTransferSyntax.JPEGProcess14,  //57
                DicomTransferSyntax.JPEGProcess14SV1, //70
                DicomTransferSyntax.JPEGLSLossless, //80
                DicomTransferSyntax.RLELossless
            };

            // Build the set of accepted SOP Classes and their transfer syntaxes
            return new Dictionary<DicomUID, DicomTransferSyntax[]>
            {
                { DicomUID.Verification, acceptedVerificationSyntaxes },
                { DicomUID.RTStructureSetStorage, acceptedRTransferSyntaxs },
                { DicomUID.CTImageStorage, acceptedImageTransferSyntaxs },
                { DicomUID.MRImageStorage, acceptedImageTransferSyntaxs }
            };
        }

        /// <summary>
        /// Create a new instance of the <see cref="ConfigurationService"/> class.
        /// </summary>
        /// <param name="innerEyeSegmentationClient">Optional InnerEye segmentation client.</param>
        /// <param name="getConfigurationServiceConfig">Configuration service config callback.</param>
        /// <param name="services">The services.</param>
        /// <returns>New ConfigurationService<T>.</returns>
        protected ConfigurationService CreateConfigurationService(
            IInnerEyeSegmentationClient innerEyeSegmentationClient = null,
            Func<ConfigurationServiceConfig> getConfigurationServiceConfig = null,
            params IService[] services) =>
                new ConfigurationService(
                    innerEyeSegmentationClient != null ? () => innerEyeSegmentationClient : TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient(),
                    getConfigurationServiceConfig ?? TestGatewayProcessorConfigProvider.ConfigurationServiceConfig,
                    _loggerFactory.CreateLogger("ConfigurationService"),
                    services);

        /// <summary>
        /// Create a new instance of the <see cref="DeleteService"/> class.
        /// </summary>
        /// <returns>New DeleteService.</returns>
        protected DeleteService CreateDeleteService() =>
                new DeleteService(
                    TestDeleteQueuePath,
                    TestGatewayProcessorConfigProvider.DequeueServiceConfig,
                    _loggerFactory.CreateLogger("DeleteService"));

        /// <summary>
        /// Creates a new instance of the <see cref="DownloadService"/> class.
        /// </summary>
        /// <param name="innerEyeSegmentationClient">Optional InnerEye segmentation client.</param>
        /// <param name="dequeueServiceConfig">Optional dequeue service config.</param>
        /// <param name="instances">The number of concurrent execution instances we should have.</param>
        /// <returns>New DownloadService.</returns>
        protected DownloadService CreateDownloadService(
            IInnerEyeSegmentationClient innerEyeSegmentationClient = null,
            DequeueServiceConfig dequeueServiceConfig = null,
            int instances = 1) =>
                new DownloadService(
                    innerEyeSegmentationClient != null ? () => innerEyeSegmentationClient : TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient(),
                    TestDownloadQueuePath,
                    TestPushQueuePath,
                    TestDeleteQueuePath,
                    () => new DownloadServiceConfig(),
                    dequeueServiceConfig != null ? (Func<DequeueServiceConfig>)(() => dequeueServiceConfig) : TestGatewayProcessorConfigProvider.DequeueServiceConfig,
                    _loggerFactory.CreateLogger("DownloadService"),
                    instances);

        /// <summary>
        /// Creates a new instance of the <see cref="PushService"/> class.
        /// </summary>
        /// <param name="aetConfigProvider">AET configuration provider.</param>
        /// <returns>New PushService</returns>
        protected PushService CreatePushService(
            Func<IEnumerable<AETConfigModel>> aetConfigProvider = null) =>
                new PushService(
                    aetConfigProvider ?? _testAETConfigProvider.GetAETConfigs,
                    new DicomDataSender(),
                    TestPushQueuePath,
                    TestDeleteQueuePath,
                    TestGatewayProcessorConfigProvider.DequeueServiceConfig,
                    _loggerFactory.CreateLogger("PushService"),
                    1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveService"/> class.
        /// </summary>
        /// <param name="gatewayReceiveConfigProvider">Callback to get configuration.</param>
        /// <returns>New ReceiveService.</returns>
        protected ReceiveService CreateReceiveService(
            Func<ReceiveServiceConfig> getReceiveServiceConfig) =>
                new ReceiveService(
                    getReceiveServiceConfig,
                    TestUploadQueuePath,
                    _loggerFactory.CreateLogger("ReceiveService"));

        protected ReceiveService CreateReceiveService(
            int port,
            DirectoryInfo rootDicomFolder = null) =>
                CreateReceiveService(() => GetTestGatewayReceiveServiceConfig(port, rootDicomFolder));

        /// <summary>
        /// Creates a new instance of the <see cref="UploadService"/> class.
        /// </summary>
        /// <param name="innerEyeSegmentationClient">Optional InnerEye segmentation client.</param>
        /// <param name="aetConfigProvider">AET configuration provider.</param>
        /// <param name="instances">The number of concurrent execution instances we should have.</param>
        /// <returns>New UploadService.</returns>
        protected UploadService CreateUploadService(
            IInnerEyeSegmentationClient innerEyeSegmentationClient = null,
            Func<IEnumerable<AETConfigModel>> aetConfigProvider = null,
            int instances = 1) =>
                new UploadService(
                    innerEyeSegmentationClient != null ? () => innerEyeSegmentationClient : TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient(),
                    aetConfigProvider ?? _testAETConfigProvider.GetAETConfigs,
                    TestUploadQueuePath,
                    TestDownloadQueuePath,
                    TestDeleteQueuePath,
                    TestGatewayProcessorConfigProvider.DequeueServiceConfig,
                    _loggerFactory.CreateLogger("UploadService"),
                    instances);

        protected async Task<(string SegmentationId, string ModelId, IEnumerable<byte[]> Data)> StartRealSegmentationAsync(string filesPath)
        {
            var dicomFiles = new DirectoryInfo(filesPath).GetFiles().Select(x => DicomFile.Open(x.FullName)).ToArray();

            using (var segmentationClient = TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient().Invoke())
            {
                var testAETConfigModel = GetTestAETConfigModel();

                var matchedModel = ApplyAETModelConfigProvider.ApplyAETModelConfig(testAETConfigModel.AETConfig.Config.ModelsConfig, dicomFiles);
                var modelId = matchedModel.Result.ModelId;

                var startSegmentationResult = await segmentationClient.StartSegmentationAsync(
                    matchedModel.Result.ModelId,
                    matchedModel.Result.ChannelData);

                var referenceDicomFiles = startSegmentationResult.postedImages.CreateNewDicomFileWithoutPixelData(segmentationClient.SegmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));
                return (startSegmentationResult.segmentationId, modelId, referenceDicomFiles);
            }
        }

        protected async Task<(string SegmentationId, string ModelId, IEnumerable<byte[]> Data)> StartFakeSegmentationAsync(string filesPath)
        {
            var dicomFiles = new DirectoryInfo(filesPath).GetFiles().Select(x => DicomFile.Open(x.FullName)).ToArray();

            var segmentationClient = GetMockInnerEyeSegmentationClient();
            segmentationClient.RealSegmentation = false;

            var testAETConfigModel = GetTestAETConfigModel();

            var matchedModel = ApplyAETModelConfigProvider.ApplyAETModelConfig(testAETConfigModel.AETConfig.Config.ModelsConfig, dicomFiles);
            var modelId = matchedModel.Result.ModelId;

            var startSegmentationResult = await segmentationClient.StartSegmentationAsync(
                matchedModel.Result.ModelId,
                matchedModel.Result.ChannelData);

            var referenceDicomFiles = startSegmentationResult.postedImages.CreateNewDicomFileWithoutPixelData(segmentationClient.SegmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));
            return (startSegmentationResult.segmentationId, modelId, referenceDicomFiles);
        }

        protected static void WaitUntilNoMessagesOnQueue(IMessageQueue queue, int timeoutMs = 60000)
        {
            // Wait for all events to finish on the data received
            SpinWait.SpinUntil(() =>
            {
                using (var messageQueueTransaction = queue.CreateQueueTransaction())
                {
                    messageQueueTransaction.Begin();

                    try
                    {
                        queue.DequeueNextMessage<QueueItemBase>(messageQueueTransaction);
                        messageQueueTransaction.Abort();

                        Task.WaitAll(Task.Delay(500));

                        return false;
                    }
                    catch (MessageQueueReadException)
                    {
                        messageQueueTransaction.Abort();

                        return true;
                    }
                }
            },
            TimeSpan.FromMilliseconds(timeoutMs));
        }

        protected static void Enqueue<T>(IMessageQueue queue, T message, bool clearQueue)
        {
            if (clearQueue)
            {
                queue.Clear();
            }

            using (var queueTransaction = queue.CreateQueueTransaction())
            {
                queueTransaction.Begin();
                queue.Enqueue(message, queueTransaction);
                queueTransaction.Commit();
            }
        }

        private void TryKillAnyZombieProcesses()
        {
            var zombieProcesses = Process.GetProcesses()?.Where(x => x.ProcessName == "storescu")?.ToList();

            foreach (var process in zombieProcesses)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception e)
                {
                    TestContext.WriteLine($"Failed to kill process {process.Id}, {process.ProcessName} with exception {e}");
                }
            }
        }
    }
}