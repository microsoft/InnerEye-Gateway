namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.IO;
    using System.Threading;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.DicomConstraints;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.Tests.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UploadServiceTests : BaseTestClass
    {
        [TestCategory("UploadService")]
        [Description("Tests to the upload service with a strange config (duplicate model in the same config).")]
        [Timeout(60 * 1000)]
        [TestMethod]
        public void StrangeConfigUploadServiceTest()
        {
            var segmentationClient = GetMockInnerEyeSegmentationClient();
            segmentationClient.RealSegmentation = false;

            var testAETConfigModel = GetTestAETConfigModel();
            var newTestAETConfigModel = testAETConfigModel.With(
                aetConfig: new ClientAETConfig(
                new AETConfig(
                    AETConfigType.Model,
                    new[]
                    {
                        new ModelConstraintsConfig(
                            "b033d049-0233-4068-bc0f-c64cec48e8fa",
                            new [] { new ModelChannelConstraints("ct", new GroupConstraint(new DicomConstraint[0], LogicalOperator.And), new GroupConstraint(new DicomConstraint[0], LogicalOperator.And), -1, -1) },
                            new TagReplacement[0]),
                        new ModelConstraintsConfig(
                            "b033d049-0233-4068-bc0f-c64cec48e8fa",
                            new [] { new ModelChannelConstraints("ct", new GroupConstraint(new DicomConstraint[0], LogicalOperator.And), new GroupConstraint(new DicomConstraint[0], LogicalOperator.And), -1, -1) },
                            new TagReplacement[0]),
                    }),
                    testAETConfigModel.AETConfig.Destination,
                    false));

            var aetConfigProvider = new MockAETConfigProvider(newTestAETConfigModel);

            using (var deleteService = CreateDeleteService())
            using (var uploadService = CreateUploadService(segmentationClient, aetConfigProvider.AETConfigModels))
            using (var uploadQueue = uploadService.UploadQueue)
            using (var downloadQueue = uploadService.DownloadQueue)
            {
                deleteService.Start();
                uploadService.Start();

                var tempFolder = CreateTemporaryDirectory();

                foreach (var file in new DirectoryInfo(@"Images\1ValidSmall\").GetFiles())
                {
                    file.CopyTo(Path.Combine(tempFolder.FullName, file.Name));
                }

                Enqueue(
                    uploadQueue,
                    new UploadQueueItem(
                        calledApplicationEntityTitle: newTestAETConfigModel.CalledAET,
                        callingApplicationEntityTitle: newTestAETConfigModel.CallingAET,
                        associationFolderPath: tempFolder.FullName,
                        rootDicomFolderPath: tempFolder.FullName,
                        associationGuid: Guid.NewGuid(),
                        associationDateTime: DateTime.UtcNow),
                    true);

                // Leave enough time to allow upload
                SpinWait.SpinUntil(() => new DirectoryInfo(tempFolder.FullName).Exists == false, TimeSpan.FromSeconds(300));

                Assert.IsFalse(new DirectoryInfo(tempFolder.FullName).Exists);

                TransactionalDequeue<DownloadQueueItem>(downloadQueue);
            }
        }

        [TestCategory("UploadService")]
        [Description("Test the happy end to end path of the upload service using the live segmentation service.")]
        [Timeout(180 * 1000)]
        [TestMethod]
        public void UploadServiceMockEndToEndTest()
        {
            var testAETConfigModel = GetTestAETConfigModel();

            var tempFolder = CreateTemporaryDirectory();

            foreach (var file in new DirectoryInfo(@"Images\LargeSeriesWithContour\").GetFiles())
            {
                file.CopyTo(Path.Combine(tempFolder.FullName, file.Name));
            }

            var segmentationClient = GetMockInnerEyeSegmentationClient();
            segmentationClient.RealSegmentation = false;

            using (var deleteService = CreateDeleteService())
            using (var downloadService = CreateDownloadService(segmentationClient, OneMinSecs))
            using (var uploadService = CreateUploadService(segmentationClient))
            using (var downloadQueue = downloadService.DownloadQueue)
            using (var uploadQueue = uploadService.UploadQueue)
            {
                downloadQueue.Clear();

                deleteService.Start();
                downloadService.Start();
                uploadService.Start();

                Enqueue(
                    uploadQueue,
                    new UploadQueueItem(
                        calledApplicationEntityTitle: testAETConfigModel.CalledAET,
                        callingApplicationEntityTitle: testAETConfigModel.CallingAET,
                        associationFolderPath: tempFolder.FullName,
                        rootDicomFolderPath: tempFolder.FullName,
                        associationGuid: Guid.NewGuid(),
                        associationDateTime: DateTime.UtcNow),
                    true);

                // Leave enough time to allow upload
                SpinWait.SpinUntil(() => new DirectoryInfo(tempFolder.FullName).Exists == false);

                Assert.IsFalse(new DirectoryInfo(tempFolder.FullName).Exists);

                WaitUntilNoMessagesOnQueue(uploadQueue);

                // Make sure the item was dequeued
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue));

                WaitUntilNoMessagesOnQueue(downloadQueue);

                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<DownloadQueueItem>(downloadQueue));

                uploadService.OnStop();

                // Allow a few seconds for the execution thread to exit.
                SpinWait.SpinUntil(() => uploadService.IsExecutionThreadRunning == false);

                Assert.IsFalse(uploadService.IsExecutionThreadRunning);
            }
        }

        [TestCategory("UploadService")]
        [Description("Test the happy end to end path of the upload service using the live segmentation service and pushing an image with the result.")]
        [Timeout(180 * 1000)]
        [TestMethod]
        public void UploadServiceLiveSendImageTest()
        {
            var tempFolder = CreateTemporaryDirectory();

            foreach (var file in new DirectoryInfo(@"Images\1ValidSmall\").GetFiles())
            {
                file.CopyTo(Path.Combine(tempFolder.FullName, file.Name));
            }

            var segmentationClient = GetMockInnerEyeSegmentationClient();
            segmentationClient.RealSegmentation = false;

            var testAETConfigModel = GetTestAETConfigModel();
            var newTestAETConfigModel = testAETConfigModel.With(
                aetConfig: new ClientAETConfig(
                new AETConfig(
                    AETConfigType.Model,
                    new[]
                    {
                        new ModelConstraintsConfig(
                            "b033d049-0233-4068-bc0f-c64cec48e8fa",
                            new [] { new ModelChannelConstraints("ct", new GroupConstraint(new DicomConstraint[0], LogicalOperator.And), new GroupConstraint(new DicomConstraint[0], LogicalOperator.And), -1, -1) },
                            new TagReplacement[0]),
                    }),
                    testAETConfigModel.AETConfig.Destination,
                    true));

            var aetConfigProvider = new MockAETConfigProvider(newTestAETConfigModel);

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
                    newTestAETConfigModel.AETConfig.Destination.Port,
                    BuildAcceptedSopClassesAndTransferSyntaxes,
                    TimeSpan.FromSeconds(5));

                Assert.IsTrue(result);

                using (var deleteService = CreateDeleteService())
                using (var uploadService = CreateUploadService(segmentationClient, aetConfigProvider.AETConfigModels))
                using (var uploadQueue = uploadService.UploadQueue)
                using (var downloadService = CreateDownloadService(segmentationClient, OneMinSecs))
                using (var downloadQueue = downloadService.DownloadQueue)
                using (var pushService = CreatePushService(aetConfigProvider.AETConfigModels))
                using (var pushQueue = pushService.PushQueue)
                {
                    deleteService.Start();
                    uploadService.Start();
                    downloadService.Start();
                    pushService.Start();

                    Enqueue(
                        uploadQueue,
                        new UploadQueueItem(
                            calledApplicationEntityTitle: newTestAETConfigModel.CalledAET,
                            callingApplicationEntityTitle: newTestAETConfigModel.CallingAET,
                            associationFolderPath: tempFolder.FullName,
                            rootDicomFolderPath: CreateTemporaryDirectory().FullName,
                            associationGuid: Guid.NewGuid(),
                            associationDateTime: DateTime.UtcNow),
                        true);

                    // Leave enough time to allow upload
                    SpinWait.SpinUntil(() => new DirectoryInfo(tempFolder.FullName).Exists == false, TimeSpan.FromSeconds(90));

                    // Check the association folder is deleted.
                    Assert.IsFalse(new DirectoryInfo(tempFolder.FullName).Exists);

                    // Wait for all events to finish on the data received
                    SpinWait.SpinUntil(() => eventCount >= 3, TimeSpan.FromMinutes(3));

                    // Make sure the item was dequeued
                    Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<UploadQueueItem>(uploadQueue));
                    Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<DownloadQueueItem>(downloadQueue));
                    Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<PushQueueItem>(pushQueue));

                    Assert.IsFalse(string.IsNullOrWhiteSpace(folderPath));

                    // Check we received the image in the result
                    Assert.AreEqual(21, new DirectoryInfo(folderPath).GetFiles().Length);
                }
            }
        }
    }
}