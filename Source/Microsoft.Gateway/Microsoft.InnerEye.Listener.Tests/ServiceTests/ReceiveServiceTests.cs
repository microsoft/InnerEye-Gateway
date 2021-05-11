namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common.Providers;
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

            var mockConfigurationServiceConfigProvider = new MockConfigurationProvider<ConfigurationServiceConfig>();

            var configurationServiceConfig1 = new ConfigurationServiceConfig(
                configurationRefreshDelaySeconds: 1);

            var configurationServiceConfig2 = new ConfigurationServiceConfig(
                configurationServiceConfig1.ConfigCreationDateTime.AddSeconds(5),
                configurationServiceConfig1.ApplyConfigDateTime.AddSeconds(10));

            mockConfigurationServiceConfigProvider.ConfigurationQueue.Clear();
            mockConfigurationServiceConfigProvider.ConfigurationQueue.Enqueue(configurationServiceConfig1);
            mockConfigurationServiceConfigProvider.ConfigurationQueue.Enqueue(configurationServiceConfig2);

            var mockReceiverConfigurationProvider2 = new MockConfigurationProvider<ReceiveServiceConfig>();

            var testReceiveServiceConfig1 = GetTestGatewayReceiveServiceConfig(110);
            var testReceiveServiceConfig2 = GetTestGatewayReceiveServiceConfig(111);

            mockReceiverConfigurationProvider2.ConfigurationQueue.Clear();
            mockReceiverConfigurationProvider2.ConfigurationQueue.Enqueue(testReceiveServiceConfig1);
            mockReceiverConfigurationProvider2.ConfigurationQueue.Enqueue(testReceiveServiceConfig2);

            using (var client = GetMockInnerEyeSegmentationClient())
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

            var gatewayReceiveConfig = GetTestGatewayReceiveServiceConfig(140);

            mockReceiverConfigurationProvider.ConfigurationQueue.Clear();
            mockReceiverConfigurationProvider.ConfigurationQueue.Enqueue(gatewayReceiveConfig);

            using (var receiveService = CreateReceiveService(mockReceiverConfigurationProvider.GetConfiguration))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                receiveService.Start();

                // This should cause an exception to be raised in ReceiveService.GetAcceptedSopClassesAndTransferSyntaxes
                mockReceiverConfigurationProvider.TestException = new ConfigurationException("A general exception.");

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
            var receivePort = 160;

            var callingAet = "ProstateRTMl";
            var calledAet = "testname";

            using (var receiveService = CreateReceiveService(receivePort))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                uploadQueue.Clear(); // Clear the message queue
                receiveService.Start(); // Start the service

                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    receivePort,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: callingAet,
                    calledAETitle: calledAet);

                var receiveQueueItem = TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 20 * 1000);

                Assert.IsNotNull(receiveQueueItem);
                Assert.AreEqual(callingAet, receiveQueueItem.CallingApplicationEntityTitle);
                Assert.AreEqual(calledAet, receiveQueueItem.CalledApplicationEntityTitle);

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
            var calledAet = "testname";
            var receivePort = 180;

            using (var receiveService = CreateReceiveService(receivePort))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                uploadQueue.Clear();
                receiveService.Start();

                var sender = new DicomDataSender();

                await sender.DicomEchoAsync(
                    "Hello",
                    calledAet,
                    receivePort,
                    "127.0.0.1");

                // Check nothing is added to the message queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));

                // Now check when we send a file a message is added
                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    receivePort,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: "ProstateRTMl",
                    calledAETitle: calledAet);

                Assert.IsNotNull(TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));

                // Now try another Dicom echo
                await sender.DicomEchoAsync(
                    "Hello",
                    calledAet,
                    receivePort,
                    "127.0.0.1");

                // Check nothing is added to the message queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));
            }
        }
    }
}