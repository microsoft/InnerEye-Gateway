namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    [TestClass]
    public class DownloadServiceTests : BaseTestClass
    {
        private readonly TagReplacement[] _defaultTagReplacement = new[]
        {
            new TagReplacement(TagReplacementOperation.UpdateIfExists, new DicomConstraints.DicomTagIndex(Dicom.DicomTag.Manufacturer), "MicrosoftCorp")
        };

        [TestCategory("DownloadService")]
        [Ignore("Integration test, relies on live API")]
        [Description("Test the happy end to end path of the download service using the live segmentation service.")]
        [Timeout(240 * 1000)]
        [TestMethod]
        public async Task DownloadServiceLiveCloudMockConfigEndToEndTest()
        {
            var resultDirectory = CreateTemporaryDirectory();

            var (segmentationId, modelId, data) = await StartRealSegmentationAsync(@"Images\1ValidSmall\");
            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 140, "localhost");

            // Create a Data receiver to receive the RT struct result
            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(CreateTemporaryDirectory().FullName)))
            {
                var eventCount = 0;
                var folderPath = string.Empty;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    folderPath = e.FolderPath;
                    Interlocked.Increment(ref eventCount);
                };

                StartDicomDataReceiver(dicomDataReceiver, applicationEntity.Port);

                using (var pushService = CreatePushService())
                using (var downloadService = CreateDownloadService())
                using (var downloadQueue = downloadService.DownloadQueue)
                {
                    pushService.Start();
                    downloadService.Start();

                    TransactionalEnqueue(
                        downloadQueue,
                        new DownloadQueueItem(
                            segmentationId: segmentationId,
                            modelId: modelId,
                            resultsDirectory: resultDirectory.FullName,
                            referenceDicomFiles: data,
                            calledApplicationEntityTitle: applicationEntity.Title,
                            callingApplicationEntityTitle: applicationEntity.Title,
                            destinationApplicationEntity: applicationEntity,
                            tagReplacementJsonString: JsonConvert.SerializeObject(_defaultTagReplacement),
                            associationGuid: Guid.NewGuid(),
                            associationDateTime: DateTime.UtcNow,
                            isDryRun: false));

                    // Wait for all events to finish on the data received
                    SpinWait.SpinUntil(() => eventCount >= 3, TimeSpan.FromMinutes(3));
                }

                dicomDataReceiver.StopServer();

                Assert.IsFalse(string.IsNullOrWhiteSpace(folderPath));

                var files = new DirectoryInfo(folderPath).GetFiles();

                // Check we have a file
                Assert.AreEqual(1, files.Length);

                var dicomFile = await DicomFile.OpenAsync(files[0].FullName);

                Assert.IsNotNull(dicomFile);

                TryDeleteDirectory(folderPath);
            }
        }

        [TestCategory("SystemTest")]
        [Description("Creates the download service and makes sure the service dequeues the transactions on API errors.")]
        [Timeout(60 * 1000)]
        [TestMethod]
        public void DownloadServiceAPIError()
        {
            var segmentationAnonymisationProtocol = SegmentationAnonymisationProtocol();

            var referenceDicomFiles = new DirectoryInfo(@"Images\1ValidSmall\")
                                                .GetFiles()
                                                .Select(x => DicomFile.Open(x.FullName))
                                                .CreateNewDicomFileWithoutPixelData(segmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));

            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 141, "127.0.0.1");

            using (var mockSegmentationClient = GetMockInnerEyeSegmentationClient())
            using (var downloadService = CreateDownloadService(mockSegmentationClient))
            using (var downloadQueue = downloadService.DownloadQueue)
            using (var deadLetterQueue = downloadService.DeadletterMessageQueue)
            {
                // Set the client to always return 50%
                mockSegmentationClient.SegmentationProgressResult = new ModelResult(100, "An API error.", null);

                downloadService.Start();

                Assert.IsTrue(downloadService.IsExecutionThreadRunning);

                TransactionalEnqueue(
                        downloadQueue,
                        new DownloadQueueItem(
                            segmentationId: Guid.NewGuid().ToString(),
                            modelId: Guid.NewGuid().ToString(),
                            resultsDirectory: CreateTemporaryDirectory().FullName,
                            referenceDicomFiles: referenceDicomFiles,
                            calledApplicationEntityTitle: applicationEntity.Title,
                            callingApplicationEntityTitle: applicationEntity.Title,
                            destinationApplicationEntity: applicationEntity,
                            tagReplacementJsonString: JsonConvert.SerializeObject(_defaultTagReplacement),
                            associationGuid: Guid.NewGuid(),
                            associationDateTime: DateTime.UtcNow,
                            isDryRun: false));

                WaitUntilNoMessagesOnQueue(downloadQueue);

                // Check this message has been de-queued
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<DownloadQueueItem>(downloadQueue));
                Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<DownloadQueueItem>(deadLetterQueue));
            }
        }

        [TestCategory("SystemTest")]
        [Description("Tests the download service cleans up images after failing to process a queue item.")]
        [Timeout(60 * 1000)]
        [TestMethod]
        public void TestBadApiWhenAttemptingToSendImageWithResultsTest()
        {
            var segmentationAnonymisationProtocol = SegmentationAnonymisationProtocol();

            var resultsDirectory = CreateTemporaryDirectory();

            // Copy files to result directory
            new FileInfo(Directory.EnumerateFiles(@"Images\1ValidSmall\")
                .ToList()[0]).CopyTo(Path.Combine(resultsDirectory.FullName, $"{Guid.NewGuid()}.dcm"));

            var referenceDicomFiles = resultsDirectory
                                                .GetFiles()
                                                .Select(x => DicomFile.Open(x.FullName))
                                                .CreateNewDicomFileWithoutPixelData(segmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));

            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 141, "127.0.0.1");

            using (var mockSegmentationClient = GetMockInnerEyeSegmentationClient())
            using (var deleteService = CreateDeleteService())
            using (var downloadService = CreateDownloadService(mockSegmentationClient, GetTestDequeueServiceConfig(maximumQueueMessageAgeSeconds: 1)))
            using (var downloadQueue = downloadService.DownloadQueue)
            {
                // Set the client to always return 50%
                mockSegmentationClient.SegmentationResultException = new Exception();

                deleteService.Start();
                downloadService.Start();

                Assert.IsTrue(downloadService.IsExecutionThreadRunning);

                TransactionalEnqueue(
                        downloadQueue,
                        new DownloadQueueItem(
                            segmentationId: Guid.NewGuid().ToString(),
                            modelId: Guid.NewGuid().ToString(),
                            resultsDirectory: resultsDirectory.FullName,
                            referenceDicomFiles: referenceDicomFiles,
                            calledApplicationEntityTitle: applicationEntity.Title,
                            callingApplicationEntityTitle: applicationEntity.Title,
                            destinationApplicationEntity: applicationEntity,
                            tagReplacementJsonString: JsonConvert.SerializeObject(_defaultTagReplacement),
                            associationGuid: Guid.NewGuid(),
                            associationDateTime: DateTime.UtcNow,
                            isDryRun: false));

                SpinWait.SpinUntil(() => new DirectoryInfo(resultsDirectory.FullName).Exists == false, TimeSpan.FromSeconds(60));

                Assert.IsFalse(new DirectoryInfo(resultsDirectory.FullName).Exists);
            }
        }

        [TestCategory("SystemTest")]
        [Description("Tests the download service stops when requested whilst stuck waiting for a download to complete.")]
        [Timeout(60 * 1000)]
        [TestMethod]
        public void DownloadServiceExitsCorrectlyTest()
        {
            var segmentationAnonymisationProtocol = SegmentationAnonymisationProtocol();

            var referenceDicomFiles = new DirectoryInfo(@"Images\1ValidSmall\")
                                                .GetFiles()
                                                .Select(x => DicomFile.Open(x.FullName))
                                                .CreateNewDicomFileWithoutPixelData(segmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));

            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 142, "127.0.0.1");

            using (var mockSegmentationClient = GetMockInnerEyeSegmentationClient())
            using (var downloadService = CreateDownloadService(mockSegmentationClient))
            using (var downloadQueue = downloadService.DownloadQueue)
            {
                // Set the client to always return 50%
                mockSegmentationClient.SegmentationProgressResult = new ModelResult(50, string.Empty, null);

                TransactionalEnqueue(
                       downloadQueue,
                       new DownloadQueueItem(
                           segmentationId: Guid.NewGuid().ToString(),
                           modelId: Guid.NewGuid().ToString(),
                           resultsDirectory: CreateTemporaryDirectory().FullName,
                           referenceDicomFiles: referenceDicomFiles,
                           calledApplicationEntityTitle: applicationEntity.Title,
                           callingApplicationEntityTitle: applicationEntity.Title,
                           destinationApplicationEntity: applicationEntity,
                           tagReplacementJsonString: JsonConvert.SerializeObject(_defaultTagReplacement),
                           associationGuid: Guid.NewGuid(),
                           associationDateTime: DateTime.UtcNow,
                           isDryRun: false));

                downloadService.Start();

                SpinWait.SpinUntil(() => downloadService.IsExecutionThreadRunning);

                WaitUntilNoMessagesOnQueue(downloadQueue);

                Assert.IsTrue(downloadService.IsExecutionThreadRunning);

                downloadService.OnStop();

                SpinWait.SpinUntil(() => !downloadService.IsExecutionThreadRunning);

                Assert.IsFalse(downloadService.IsExecutionThreadRunning);
            }
        }

        [TestCategory("DownloadService")]
        [Description("Test the download service does not lose data when there is an error")]
        [Timeout(240 * 1000)]
        [TestMethod]
        public void DownloadServiceInvalidDownloadId()
        {
            var segmentationAnonymisationProtocol = SegmentationAnonymisationProtocol();

            var referenceDicomFiles = new DirectoryInfo(@"Images\1ValidSmall\")
                                                    .GetFiles()
                                                    .Select(x => DicomFile.Open(x.FullName))
                                                    .CreateNewDicomFileWithoutPixelData(segmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));

            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 143, "127.0.0.1");

            using (var downloadService = CreateDownloadService(dequeueServiceConfig: GetTestDequeueServiceConfig(deadLetterMoveFrequencySeconds: 1000)))
            using (var downloadQueue = downloadService.DownloadQueue)
            using (var deadLetterQueue = downloadService.DeadletterMessageQueue)
            {
                downloadService.Start();

                TransactionalEnqueue(
                    downloadQueue,
                    new DownloadQueueItem(
                        segmentationId: Guid.NewGuid().ToString(),
                        modelId: Guid.NewGuid().ToString(),
                        resultsDirectory: CreateTemporaryDirectory().FullName,
                        referenceDicomFiles: referenceDicomFiles,
                        calledApplicationEntityTitle: applicationEntity.Title,
                        callingApplicationEntityTitle: applicationEntity.Title,
                        destinationApplicationEntity: applicationEntity,
                        tagReplacementJsonString: JsonConvert.SerializeObject(_defaultTagReplacement),
                        associationGuid: Guid.NewGuid(),
                        associationDateTime: DateTime.UtcNow,
                        isDryRun: false));

                var message = TransactionalDequeue<DownloadQueueItem>(deadLetterQueue, timeoutMs: 60 * 1000);

                Assert.IsNotNull(message);
            }
        }

        [TestCategory("DownloadService")]
        [Description("Test the download service recovers from the API going down.")]
        [Timeout(240 * 1000)]
        [TestMethod]
        public async Task DownloadServiceNoApiConnection()
        {
            var (segmentationId, modelId, data) = await StartFakeSegmentationAsync(@"Images\1ValidSmall\");
            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 144, "127.0.0.1");

            // Create a Data receiver to receive the RT struct result
            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(CreateTemporaryDirectory().FullName)))
            {
                var eventCount = 0;
                var folderPath = string.Empty;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    folderPath = e.FolderPath;
                    Interlocked.Increment(ref eventCount);
                };

                StartDicomDataReceiver(dicomDataReceiver, applicationEntity.Port);

                using (var mockSegmentationClient = GetMockInnerEyeSegmentationClient())
                using (var pushService = CreatePushService())
                using (var downloadService = CreateDownloadService(mockSegmentationClient))
                using (var downloadQueue = downloadService.DownloadQueue)
                using (var deadLetterQueue = downloadService.DeadletterMessageQueue)
                {
                    // Fake a no response when getting progress
                    mockSegmentationClient.SegmentationResultException = new HttpRequestException();

                    pushService.Start();
                    downloadService.Start();

                    downloadQueue.Clear();

                    TransactionalEnqueue(
                        downloadQueue,
                        new DownloadQueueItem(
                            segmentationId: segmentationId,
                            modelId: modelId,
                            resultsDirectory: CreateTemporaryDirectory().FullName,
                            referenceDicomFiles: data,
                            calledApplicationEntityTitle: applicationEntity.Title,
                            callingApplicationEntityTitle: applicationEntity.Title,
                            destinationApplicationEntity: applicationEntity,
                            tagReplacementJsonString: JsonConvert.SerializeObject(_defaultTagReplacement),
                            associationGuid: Guid.NewGuid(),
                            associationDateTime: DateTime.UtcNow,
                            isDryRun: false));

                    // Wait
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    // Null the exception from the mock client
                    mockSegmentationClient.SegmentationResultException = null;

                    // Wait for all events to finish on the data received
                    SpinWait.SpinUntil(() => eventCount >= 3, TimeSpan.FromMinutes(5));

                    WaitUntilNoMessagesOnQueue(downloadQueue);

                    // Check this message has been de-queued
                    Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<DownloadQueueItem>(downloadQueue));

                    Assert.ThrowsException<MessageQueueReadException>(() => TransactionalDequeue<DownloadQueueItem>(deadLetterQueue));
                }

                dicomDataReceiver.StopServer();

                Assert.IsFalse(string.IsNullOrWhiteSpace(folderPath));

                var files = new DirectoryInfo(folderPath).GetFiles();

                // Check we have a file
                Assert.AreEqual(1, files.Length);

                var dicomFile = await DicomFile.OpenAsync(files[0].FullName);

                Assert.IsNotNull(dicomFile);

                TryDeleteDirectory(folderPath);
            }
        }
    }
}
