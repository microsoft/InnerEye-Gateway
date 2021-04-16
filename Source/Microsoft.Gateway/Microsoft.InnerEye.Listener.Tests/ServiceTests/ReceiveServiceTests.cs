namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.Tests.Common.Helpers;
    using Microsoft.InnerEye.Listener.Tests.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ReceiveServiceTests : BaseTestClass
    {
        [TestCategory("ReceiveServiceDCMTK")]
        [Description("Checks the receive service restarts with a new configuration.")]
        [Timeout(60000)]
        [TestMethod]
        public void ReceiveServiceRestartTest()
        {
            var callingAet = "ProstateRTMl";

            var client = GetMockInnerEyeSegmentationClient();
            var resultDirectory = CreateTemporaryDirectory();
            var mockConfigurationServiceConfigProvider = new MockConfigurationProvider<ConfigurationServiceConfig>();

            var gatewayConfig = GetTestGatewayReceiveConfig();

            var configurationServiceConfig1 = new ConfigurationServiceConfig(
                configurationRefreshDelaySeconds: 1);

            var configurationServiceConfig2 = new ConfigurationServiceConfig(
                configurationServiceConfig1.ConfigCreationDateTime.AddSeconds(5),
                configurationServiceConfig1.ApplyConfigDateTime.AddSeconds(10));

            mockConfigurationServiceConfigProvider.ConfigurationQueue.Clear();
            mockConfigurationServiceConfigProvider.ConfigurationQueue.Enqueue(configurationServiceConfig1);
            mockConfigurationServiceConfigProvider.ConfigurationQueue.Enqueue(configurationServiceConfig2);

            var mockReceiverConfigurationProvider2 = new MockConfigurationProvider<ReceiveServiceConfig>();

            var testReceiveServiceConfig = GetTestGatewayReceiveConfig();

            var testReceiveServiceConfig1 = gatewayConfig.With(
                new DicomEndPoint("TestAET1", 110, "127.0.0.1"),
                resultDirectory.FullName);

            var testReceiveServiceConfig2 = gatewayConfig.With(
                new DicomEndPoint("TestAET2", 111, "127.0.0.1"),
                resultDirectory.FullName);

            mockReceiverConfigurationProvider2.ConfigurationQueue.Clear();
            mockReceiverConfigurationProvider2.ConfigurationQueue.Enqueue(testReceiveServiceConfig1);
            mockReceiverConfigurationProvider2.ConfigurationQueue.Enqueue(testReceiveServiceConfig2);

            using (var receiveService = CreateReceiveService(mockReceiverConfigurationProvider2.GetConfiguration))
            using (var uploadQueue = receiveService.UploadQueue)
            using (var configurationService = CreateConfigurationService(
                client,
                mockConfigurationServiceConfigProvider.GetConfiguration,
                receiveService))
            {
                // Start the service
                configurationService.Start();

                uploadQueue.Clear(); // Clear the message queue

                SpinWait.SpinUntil(() => receiveService.StartCount == 2);

                // Send on the new config
                var result = DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    testReceiveServiceConfig1.GatewayDicomEndPoint.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: callingAet,
                    calledAETitle: testReceiveServiceConfig1.GatewayDicomEndPoint.Title);

                // Check this did send on the old config
                Assert.IsFalse(string.IsNullOrWhiteSpace(result));

                // Send on the new config
                result = DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    testReceiveServiceConfig2.GatewayDicomEndPoint.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: callingAet,
                    calledAETitle: testReceiveServiceConfig2.GatewayDicomEndPoint.Title);

                // Check this did send on the new config
                Assert.IsTrue(string.IsNullOrWhiteSpace(result));

                var receiveQueueItem = TransactionalDequeue<UploadQueueItem>(uploadQueue);

                Assert.IsNotNull(receiveQueueItem);
                Assert.AreEqual(callingAet, receiveQueueItem.CallingApplicationEntityTitle);
                Assert.AreEqual(testReceiveServiceConfig2.GatewayDicomEndPoint.Title, receiveQueueItem.CalledApplicationEntityTitle);

                Assert.IsFalse(string.IsNullOrEmpty(receiveQueueItem.AssociationFolderPath));

                var saveDirectoryInfo = new DirectoryInfo(receiveQueueItem.AssociationFolderPath);

                Assert.IsTrue(saveDirectoryInfo.Exists);

                var files = saveDirectoryInfo.GetFiles();

                // Check we received one file over this association
                Assert.AreEqual(1, files.Length);

                // Attempt to get another item from the queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue));
            }
        }

        [TestCategory("ReceiveServiceDCMTK")]
        [Description("Checks the receive service continues accepting data even when the API goes down.")]
        [Timeout(60000)]
        [TestMethod]
        public void ReceiveServiceAPIDownTest()
        {
            var callingAet = "ProstateRTMl";

            var mockReceiverConfigurationProvider = new MockConfigurationProvider<ReceiveServiceConfig>();
            var client = GetMockInnerEyeSegmentationClient();

            var gatewayReceiveConfig = GetTestGatewayReceiveConfig().With(
                new DicomEndPoint("Gateway", 140, "localhost"));

            mockReceiverConfigurationProvider.ConfigurationQueue.Clear();
            mockReceiverConfigurationProvider.ConfigurationQueue.Enqueue(gatewayReceiveConfig);

            using (var receiveService = CreateReceiveService(mockReceiverConfigurationProvider.GetConfiguration))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                receiveService.Start();

                // This should cause an exception to be raised in ReceiveService.GetAcceptedSopClassesAndTransferSyntaxes
                mockReceiverConfigurationProvider.TestException = new Exception("A general exception.");

                uploadQueue.Clear();

                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    gatewayReceiveConfig.GatewayDicomEndPoint.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: callingAet,
                    calledAETitle: gatewayReceiveConfig.GatewayDicomEndPoint.Title);

                var receiveQueueItem = TransactionalDequeue<UploadQueueItem>(uploadQueue, 10000);

                Assert.IsNotNull(receiveQueueItem);
                Assert.AreEqual(callingAet, receiveQueueItem.CallingApplicationEntityTitle);
                Assert.AreEqual(gatewayReceiveConfig.GatewayDicomEndPoint.Title, receiveQueueItem.CalledApplicationEntityTitle);

                Assert.IsFalse(string.IsNullOrEmpty(receiveQueueItem.AssociationFolderPath));

                var saveDirectoryInfo = new DirectoryInfo(receiveQueueItem.AssociationFolderPath);

                Assert.IsTrue(saveDirectoryInfo.Exists);

                var files = saveDirectoryInfo.GetFiles();

                // Check we received one file over this association
                Assert.AreEqual(1, files.Length);

                // Attempt to get another item from the queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue));
            }
        }

        [TestCategory("ReceiveServiceDCMTK")]
        [Description("Checks a valid end to end test of the receiver service by sending an item and picking it off the receive queue.")]
        [Timeout(60000)]
        [TestMethod]
        public void ReceiveServiceLiveEndToEndTest()
        {
            var callingAet = "ProstateRTMl";

            var config = TestGatewayReceiveConfigProvider.ReceiveServiceConfig();
            var gatewayReceiveConfig = config.With(
                new DicomEndPoint("testname", 160, "localhost"));

            using (var receiveService = CreateReceiveService(() => gatewayReceiveConfig))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                uploadQueue.Clear(); // Clear the message queue
                receiveService.Start(); // Start the service

                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    gatewayReceiveConfig.GatewayDicomEndPoint.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: callingAet,
                    calledAETitle: gatewayReceiveConfig.GatewayDicomEndPoint.Title);

                var receiveQueueItem = TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 20 * 1000);

                Assert.IsNotNull(receiveQueueItem);
                Assert.AreEqual(callingAet, receiveQueueItem.CallingApplicationEntityTitle);
                Assert.AreEqual(gatewayReceiveConfig.GatewayDicomEndPoint.Title, receiveQueueItem.CalledApplicationEntityTitle);

                Assert.IsFalse(string.IsNullOrEmpty(receiveQueueItem.AssociationFolderPath));

                var saveDirectoryInfo = new DirectoryInfo(receiveQueueItem.AssociationFolderPath);

                Assert.IsTrue(saveDirectoryInfo.Exists);

                var files = saveDirectoryInfo.GetFiles();

                // Check we received one file over this association
                Assert.AreEqual(1, files.Length);

                // Attempt to get another item from the queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue));
            }
        }

        [TestCategory("ReceiveServiceDCMTK")]
        [Description("Checks the receive service does not enqueue a message on a Dicom echo.")]
        [Timeout(60000)]
        [TestMethod]
        public async Task ReceiveServiceEchoTest()
        {
            var config = TestGatewayReceiveConfigProvider.ReceiveServiceConfig();
            var gatewayReceiveConfig = config.With(
                new DicomEndPoint("testname", 180, "localhost"));

            using (var receiveService = CreateReceiveService(() => gatewayReceiveConfig))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                uploadQueue.Clear();
                receiveService.Start();

                var sender = new DicomDataSender();

                await sender.DicomEchoAsync(
                    "Hello",
                    gatewayReceiveConfig.GatewayDicomEndPoint.Title,
                    gatewayReceiveConfig.GatewayDicomEndPoint.Port,
                    gatewayReceiveConfig.GatewayDicomEndPoint.Ip);

                // Check nothing is added to the message queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));

                // Now check when we send a file a message is added
                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    gatewayReceiveConfig.GatewayDicomEndPoint.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: "ProstateRTMl",
                    calledAETitle: gatewayReceiveConfig.GatewayDicomEndPoint.Title);

                Assert.IsNotNull(TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));

                // Now try another Dicom echo
                await sender.DicomEchoAsync(
                    "Hello",
                    gatewayReceiveConfig.GatewayDicomEndPoint.Title,
                    gatewayReceiveConfig.GatewayDicomEndPoint.Port,
                    gatewayReceiveConfig.GatewayDicomEndPoint.Ip);

                // Check nothing is added to the message queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));
            }
        }
    }
}