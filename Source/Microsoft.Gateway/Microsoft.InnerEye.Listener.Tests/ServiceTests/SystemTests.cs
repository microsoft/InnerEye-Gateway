namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Models;
    using Microsoft.InnerEye.Listener.Tests.Common.Helpers;
    using Microsoft.InnerEye.Listener.Tests.Models;
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
            var tempFolder = CreateTemporaryDirectory();

            foreach (var file in new DirectoryInfo(@"Images\1ValidSmall\").GetFiles())
            {
                file.CopyTo(Path.Combine(tempFolder.FullName, file.Name));
            }

            var client = GetMockInnerEyeSegmentationClient();
            client.RealSegmentation = false;

            var mockConfigurationServiceConfigProvider = new MockConfigurationProvider<ConfigurationServiceConfig>();

            var configurationServiceConfig1 = new ConfigurationServiceConfig(
                configurationRefreshDelaySeconds: 1);

            var configurationServiceConfig2 = new ConfigurationServiceConfig(
                configurationServiceConfig1.ConfigCreationDateTime.AddSeconds(5),
                configurationServiceConfig1.ApplyConfigDateTime.AddSeconds(10));

            mockConfigurationServiceConfigProvider.ConfigurationQueue.Enqueue(configurationServiceConfig1);
            mockConfigurationServiceConfigProvider.ConfigurationQueue.Enqueue(configurationServiceConfig2);

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

                var testAETConfigModel = GetTestAETConfigModel();
                dicomDataReceiver.StartServer(
                    testAETConfigModel.AETConfig.Destination.Port,
                    BuildAcceptedSopClassesAndTransferSyntaxes,
                    TimeSpan.FromSeconds(1));

                using (var pushService = CreatePushService())
                using (var uploadService = CreateUploadService(client))
                using (var uploadQueue = uploadService.UploadQueue)
                using (var downloadService = CreateDownloadService(client, OneHourSecs))
                using (var configurationService = CreateConfigurationService(
                    client,
                    mockConfigurationServiceConfigProvider.GetConfiguration,
                    downloadService,
                    uploadService,
                    pushService))
                {
                    // Start the service
                    configurationService.Start();

                    uploadQueue.Clear(); // Clear the message queue

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

                    Assert.IsFalse(string.IsNullOrWhiteSpace(folderPath));

                    var dicomFile = await DicomFile.OpenAsync(new DirectoryInfo(folderPath).GetFiles()[0].FullName);

                    Assert.IsNotNull(dicomFile);

                    dicomFile = null;

                    TryDeleteDirectory(folderPath);
                }
            }
        }

        [TestCategory("SystemTest")]
        [Description("Creates a service with a large delay between each execution loop. Checks that when stop is called, the service stops within 1 second.")]
        [Timeout(60 * 1000)]
        [TestMethod]
        public async Task ServiceBaseExitsCorrectlyTest()
        {
            using (var downloadService = CreateDownloadService(null, OneHourSecs))
            {
                downloadService.Start();

                await Task.Delay(1000);

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

            // Change this to real client to run a live pipeline
            var segmentationClient = GetMockInnerEyeSegmentationClient();
            segmentationClient.RealSegmentation = false;

            var gatewayReceiveConfig = GetTestGatewayReceiveConfig().With(
                new DicomEndPoint("Gateway", 141, "localhost"));

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

                var result = dicomDataReceiver.StartServer(
                    testAETConfigModel.AETConfig.Destination.Port,
                    BuildAcceptedSopClassesAndTransferSyntaxes,
                    TimeSpan.FromSeconds(1));

                Assert.IsTrue(result);

                using (var deleteService = CreateDeleteService())
                using (var pushService = CreatePushService())
                using (var downloadService = CreateDownloadService(segmentationClient, OneHourSecs))
                using (var uploadService = CreateUploadService(segmentationClient))
                using (var receiveService = CreateReceiveService(() => gatewayReceiveConfig))
                {
                    deleteService.Start();
                    pushService.Start();
                    downloadService.Start();
                    uploadService.Start();
                    receiveService.Start();

                    var dicomDataSender = new DicomDataSender();
                    var echoResult = await dicomDataSender.DicomEchoAsync(
                        testAETConfigModel.CallingAET,
                        gatewayReceiveConfig.GatewayDicomEndPoint.Title,
                        gatewayReceiveConfig.GatewayDicomEndPoint.Port,
                        gatewayReceiveConfig.GatewayDicomEndPoint.Ip);

                    Assert.IsTrue(echoResult == DicomOperationResult.Success);

                    DcmtkHelpers.SendFolderUsingDCMTK(
                        @"Images\1ValidSmall",
                        gatewayReceiveConfig.GatewayDicomEndPoint.Port,
                        ScuProfile.LEExplicitCT,
                        TestContext,
                        applicationEntityTitle: testAETConfigModel.CallingAET,
                        calledAETitle: testAETConfigModel.CalledAET);

                    // Wait for all events to finish on the data received
                    SpinWait.SpinUntil(() => eventCount >= 3, TimeSpan.FromMinutes(3));

                    Assert.IsFalse(string.IsNullOrWhiteSpace(folderPath));

                    var dicomFile = await DicomFile.OpenAsync(new DirectoryInfo(folderPath).GetFiles()[0].FullName);

                    Assert.IsNotNull(dicomFile);

                    dicomFile = null;

                    TryDeleteDirectory(folderPath);
                }
            }
        }

        [TestCategory("SystemTestDCMTK")]
        [Description("Creates all services and pushes an invalid file into the system.")]
        [Timeout(180 * 1000)]
        [TestMethod]
        public async Task GatewayBadData()
        {
            var segmentationClient = GetMockInnerEyeSegmentationClient();
            var testAETConfigModel = GetTestAETConfigModel();
            var gatewayReceiveConfig = GetTestGatewayReceiveConfig();

            using (var deleteService = CreateDeleteService())
            using (var pushService = CreatePushService())
            using (var downloadService = CreateDownloadService(segmentationClient, OneHourSecs))
            using (var uploadService = CreateUploadService(segmentationClient))
            using (var receiveService = CreateReceiveService(() => gatewayReceiveConfig))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                deleteService.Start();
                pushService.Start();
                downloadService.Start();
                uploadService.Start();
                receiveService.Start();

                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\LargeSeriesWithContour\rtstruct.dcm",
                    gatewayReceiveConfig.GatewayDicomEndPoint.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: testAETConfigModel.CallingAET,
                    calledAETitle: testAETConfigModel.CalledAET);

                await Task.Delay(1000);

                WaitUntilNoMessagesOnQueue(uploadQueue);

                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue));
            }
        }
    }
}