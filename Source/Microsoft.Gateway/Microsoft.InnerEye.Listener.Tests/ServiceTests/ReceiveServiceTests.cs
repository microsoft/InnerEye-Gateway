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
            var testAETConfigModel = GetTestAETConfigModel();

            var configurationDirectory = CreateTemporaryDirectory().FullName;

            var expectedGatewayReceiveConfig1 = TestGatewayReceiveConfigProvider.Config.With(
                receiveServiceConfig: GetTestGatewayReceiveServiceConfig(110),
                configurationServiceConfig: new ConfigurationServiceConfig(
                    configurationRefreshDelaySeconds: 1));

            ConfigurationProviderTests.Serialise(expectedGatewayReceiveConfig1, configurationDirectory, GatewayReceiveConfigProvider.GatewayReceiveConfigFileName);

            using (var client = GetMockInnerEyeSegmentationClient())
            using (var gatewayReceiveConfigProvider = CreateGatewayReceiveConfigProvider(configurationDirectory))
            using (var receiveService = CreateReceiveService(gatewayReceiveConfigProvider.ReceiveServiceConfig))
            using (var uploadQueue = receiveService.UploadQueue)
            using (var configurationService = CreateConfigurationService(
                client,
                gatewayReceiveConfigProvider.ConfigurationServiceConfig,
                receiveService))
            {
                // Start the service
                configurationService.Start();

                uploadQueue.Clear(); // Clear the message queue

                var expectedGatewayReceiveConfig2 = TestGatewayReceiveConfigProvider.Config.With(
                    receiveServiceConfig: GetTestGatewayReceiveServiceConfig(111),
                    configurationServiceConfig: new ConfigurationServiceConfig(
                        expectedGatewayReceiveConfig1.ConfigurationServiceConfig.ConfigCreationDateTime.AddSeconds(5),
                        expectedGatewayReceiveConfig1.ConfigurationServiceConfig.ApplyConfigDateTime.AddSeconds(10)));

                ConfigurationProviderTests.Serialise(expectedGatewayReceiveConfig2, configurationDirectory, GatewayReceiveConfigProvider.GatewayReceiveConfigFileName);

                SpinWait.SpinUntil(() => receiveService.StartCount == 2);

                // Send on the old config
                var result = DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    expectedGatewayReceiveConfig1.ReceiveServiceConfig.GatewayDicomEndPoint.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: testAETConfigModel.CallingAET,
                    calledAETitle: testAETConfigModel.CalledAET);

                // Check this did not send on the old config
                Assert.IsFalse(string.IsNullOrWhiteSpace(result));

                // Send on the new config
                result = DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    expectedGatewayReceiveConfig2.ReceiveServiceConfig.GatewayDicomEndPoint.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: testAETConfigModel.CallingAET,
                    calledAETitle: testAETConfigModel.CalledAET);

                // Check this did send on the new config
                Assert.IsTrue(string.IsNullOrWhiteSpace(result));

                var receiveQueueItem = TransactionalDequeue<UploadQueueItem>(uploadQueue);

                Assert.IsNotNull(receiveQueueItem);
                Assert.AreEqual(testAETConfigModel.CallingAET, receiveQueueItem.CallingApplicationEntityTitle);
                Assert.AreEqual(testAETConfigModel.CalledAET, receiveQueueItem.CalledApplicationEntityTitle);

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
            var testAETConfigModel = GetTestAETConfigModel();

            var gatewayReceiveConfig = GetTestGatewayReceiveServiceConfig(140);
            var mockReceiverConfigurationProvider = new MockConfigurationProvider<ReceiveServiceConfig>(gatewayReceiveConfig);

            using (var receiveService = CreateReceiveService(mockReceiverConfigurationProvider.Configuration))
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
                    applicationEntityTitle: testAETConfigModel.CallingAET,
                    calledAETitle: testAETConfigModel.CalledAET);

                var receiveQueueItem = TransactionalDequeue<UploadQueueItem>(uploadQueue, 10000);

                Assert.IsNotNull(receiveQueueItem);
                Assert.AreEqual(testAETConfigModel.CallingAET, receiveQueueItem.CallingApplicationEntityTitle);
                Assert.AreEqual(testAETConfigModel.CalledAET, receiveQueueItem.CalledApplicationEntityTitle);

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
            var testAETConfigModel = GetTestAETConfigModel();
            var receivePort = 160;

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
                    applicationEntityTitle: testAETConfigModel.CallingAET,
                    calledAETitle: testAETConfigModel.CalledAET);

                var receiveQueueItem = TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 20 * 1000);

                Assert.IsNotNull(receiveQueueItem);
                Assert.AreEqual(testAETConfigModel.CallingAET, receiveQueueItem.CallingApplicationEntityTitle);
                Assert.AreEqual(testAETConfigModel.CalledAET, receiveQueueItem.CalledApplicationEntityTitle);

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
            var testAETConfigModel = GetTestAETConfigModel();
            var receivePort = 180;

            using (var receiveService = CreateReceiveService(receivePort))
            using (var uploadQueue = receiveService.UploadQueue)
            {
                uploadQueue.Clear();
                receiveService.Start();

                var sender = new DicomDataSender();

                await sender.DicomEchoAsync(
                    "Hello",
                    testAETConfigModel.CalledAET,
                    receivePort,
                    "127.0.0.1").ConfigureAwait(false);

                // Check nothing is added to the message queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));

                // Now check when we send a file a message is added
                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    receivePort,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: testAETConfigModel.CallingAET,
                    calledAETitle: testAETConfigModel.CalledAET);

                Assert.IsNotNull(TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));

                // Now try another Dicom echo
                await sender.DicomEchoAsync(
                    "Hello",
                    testAETConfigModel.CalledAET,
                    receivePort,
                    "127.0.0.1").ConfigureAwait(false);

                // Check nothing is added to the message queue
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue, timeoutMs: 1000));
            }
        }
    }
}