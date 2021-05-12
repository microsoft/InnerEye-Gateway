namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Models;
    using Microsoft.InnerEye.Listener.Tests.Common.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SystemTests : BaseTestClass
    {
        [TestCategory("ProcessorService")]
        [Description("Checks the processor service restarts and continues executing correctly after restart.")]
        [Timeout(180 * 1000)]
        [TestMethod]
        public async Task ProcessorServiceRestartTest()
        {
            var configurationDirectory = CreateTemporaryDirectory().FullName;

            var expectedGatewayProcessorConfig1 = TestGatewayProcessorConfigProvider.Config.With(
                configurationServiceConfig: new ConfigurationServiceConfig(
                configurationRefreshDelaySeconds: 1));

            ConfigurationProviderTests.Serialise(expectedGatewayProcessorConfig1, configurationDirectory, GatewayProcessorConfigProvider.GatewayProcessorConfigFileName);

            var tempFolder = CreateTemporaryDirectory();

            foreach (var file in new DirectoryInfo(@"Images\1ValidSmall\").GetFiles())
            {
                file.CopyTo(Path.Combine(tempFolder.FullName, file.Name));
            }

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(CreateTemporaryDirectory().FullName)))
            {
                var eventCount = 0;
                var folderPath = string.Empty;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    folderPath = e.FolderPath;
                    Interlocked.Increment(ref eventCount);
                };

                var testAETConfigModel = GetTestAETConfigModel();

                StartDicomDataReceiver(dicomDataReceiver, testAETConfigModel.AETConfig.Destination.Port);

                using (var client = GetMockInnerEyeSegmentationClient())
                using (var pushService = CreatePushService())
                using (var uploadService = CreateUploadService(client))
                using (var uploadQueue = uploadService.UploadQueue)
                using (var downloadService = CreateDownloadService(client))
                using (var gatewayProcessorConfigProvider = CreateGatewayProcessorConfigProvider(configurationDirectory))
                using (var configurationService = CreateConfigurationService(
                    client,
                    gatewayProcessorConfigProvider.ConfigurationServiceConfig,
                    downloadService,
                    uploadService,
                    pushService))
                {
                    // Start the service
                    configurationService.Start();

                    uploadQueue.Clear(); // Clear the message queue

                    // Save a new config, this should be picked up and the services restart in 10 seconds.
                    var expectedGatewayProcessorConfig2 = TestGatewayProcessorConfigProvider.Config.With(
                        configurationServiceConfig: new ConfigurationServiceConfig(
                            expectedGatewayProcessorConfig1.ConfigurationServiceConfig.ConfigCreationDateTime.AddSeconds(5),
                            expectedGatewayProcessorConfig1.ConfigurationServiceConfig.ApplyConfigDateTime.AddSeconds(10)));

                    ConfigurationProviderTests.Serialise(expectedGatewayProcessorConfig2, configurationDirectory, GatewayProcessorConfigProvider.GatewayProcessorConfigFileName);

                    SpinWait.SpinUntil(() => pushService.StartCount == 2);
                    SpinWait.SpinUntil(() => uploadService.StartCount == 2);
                    SpinWait.SpinUntil(() => downloadService.StartCount == 2);

                    TransactionalEnqueue(
                        uploadQueue,
                        new UploadQueueItem(
                            calledApplicationEntityTitle: testAETConfigModel.CalledAET,
                            callingApplicationEntityTitle: testAETConfigModel.CallingAET,
                            associationFolderPath: tempFolder.FullName,
                            rootDicomFolderPath: tempFolder.FullName,
                            associationGuid: Guid.NewGuid(),
                            associationDateTime: DateTime.UtcNow));

                    SpinWait.SpinUntil(() => eventCount >= 3);

#pragma warning disable CA1508 // Avoid dead conditional code
                    Assert.IsFalse(string.IsNullOrWhiteSpace(folderPath));
#pragma warning restore CA1508 // Avoid dead conditional code

                    var dicomFile = await DicomFile.OpenAsync(new DirectoryInfo(folderPath).GetFiles()[0].FullName).ConfigureAwait(false);

                    Assert.IsNotNull(dicomFile);
                }
            }
        }

        [TestCategory("SystemTest")]
        [Description("Creates a service with a large delay between each execution loop. Checks that when stop is called, the service stops within 1 second.")]
        [Timeout(60 * 1000)]
        [TestMethod]
        public async Task ServiceBaseExitsCorrectlyTest()
        {
            using (var downloadService = CreateDownloadService())
            {
                downloadService.Start();

                await Task.Delay(1000).ConfigureAwait(false);

                downloadService.OnStop();

                Assert.IsFalse(downloadService.IsExecutionThreadRunning);
            }
        }

        [TestCategory("SystemTestDCMTK")]
        [Description("Creates all services and pushes an entire CT image through the end to end system.")]
        [Timeout(180 * 1000)]
        [TestMethod]
        public async Task GatewayLiveEndToEndTest()
        {
            var testAETConfigModel = GetTestAETConfigModel();

            var resultDirectory = CreateTemporaryDirectory();

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultDirectory.FullName)))
            {
                var eventCount = 0;
                var folderPath = string.Empty;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    folderPath = e.FolderPath;
                    Interlocked.Increment(ref eventCount);
                };

                StartDicomDataReceiver(dicomDataReceiver, testAETConfigModel.AETConfig.Destination.Port);

                var receivePort = 141;

                using (var segmentationClient = GetMockInnerEyeSegmentationClient())
                using (var deleteService = CreateDeleteService())
                using (var pushService = CreatePushService())
                using (var downloadService = CreateDownloadService(segmentationClient))
                using (var uploadService = CreateUploadService(segmentationClient))
                using (var receiveService = CreateReceiveService(receivePort))
                {
                    deleteService.Start();
                    pushService.Start();
                    downloadService.Start();
                    uploadService.Start();
                    receiveService.Start();

                    var dicomDataSender = new DicomDataSender();
                    var echoResult = await dicomDataSender.DicomEchoAsync(
                        testAETConfigModel.CallingAET,
                        testAETConfigModel.CalledAET,
                        receivePort,
                        "127.0.0.1").ConfigureAwait(false);

                    Assert.IsTrue(echoResult == DicomOperationResult.Success);

                    DcmtkHelpers.SendFolderUsingDCMTK(
                        @"Images\1ValidSmall",
                        receivePort,
                        ScuProfile.LEExplicitCT,
                        TestContext,
                        applicationEntityTitle: testAETConfigModel.CallingAET,
                        calledAETitle: testAETConfigModel.CalledAET);

                    // Wait for all events to finish on the data received
                    SpinWait.SpinUntil(() => eventCount >= 3, TimeSpan.FromMinutes(3));

#pragma warning disable CA1508 // Avoid dead conditional code
                    Assert.IsFalse(string.IsNullOrWhiteSpace(folderPath));
#pragma warning restore CA1508 // Avoid dead conditional code

                    var dicomFile = await DicomFile.OpenAsync(new DirectoryInfo(folderPath).GetFiles()[0].FullName).ConfigureAwait(false);

                    Assert.IsNotNull(dicomFile);
                }
            }
        }

        [TestCategory("SystemTestDCMTK")]
        [Description("Creates all services and pushes an invalid file into the system.")]
        [Timeout(180 * 1000)]
        [TestMethod]
        public async Task GatewayBadData()
        {
            var receivePort = 141;

            var testAETConfigModel = GetTestAETConfigModel();

            using (var segmentationClient = GetMockInnerEyeSegmentationClient())
            using (var deleteService = CreateDeleteService())
            using (var pushService = CreatePushService())
            using (var downloadService = CreateDownloadService(segmentationClient))
            using (var uploadService = CreateUploadService(segmentationClient))
            using (var receiveService = CreateReceiveService(receivePort))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                deleteService.Start();
                pushService.Start();
                downloadService.Start();
                uploadService.Start();
                receiveService.Start();

                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\LargeSeriesWithContour\rtstruct.dcm",
                    receivePort,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: testAETConfigModel.CallingAET,
                    calledAETitle: testAETConfigModel.CalledAET);

                await Task.Delay(1000).ConfigureAwait(false);

                WaitUntilNoMessagesOnQueue(uploadQueue);

                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue));
            }
        }
    }
}
