namespace Microsoft.InnerEye.Listener.Tests.IntegrationTests
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dicom;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Models;
    using Microsoft.InnerEye.Listener.Tests.Common.Helpers;
    using Microsoft.InnerEye.Listener.Tests.ServiceTests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EndToEndTests : BaseTestClass
    {
        /// <summary>
        /// 20 minute timeout for end-to-end test.
        /// </summary>
        public const int IntegrationTestTimeout = 20 * 60 * 1000;

        [TestCategory("IntegrationTests")]
        //[Timeout(IntegrationTestTimeout)]
        //[Ignore("Integration test, relies on live API")]
        [Description("Pushes an entire DICOM Image Series.")]
        [TestMethod]
        public async Task IntegrationTestEndToEnd()
        {
#if false
            var segmentationClient = (IInnerEyeSegmentationClient)null;// GetMockInnerEyeSegmentationClient();
            var sourceDirectory = CreateTemporaryDirectory();
            var resultDirectory = CreateTemporaryDirectory();
#else
            var segmentationClient = GetMockInnerEyeSegmentationClient();

            var sourceDirectory = new DirectoryInfo(@"Images\KeepHN");
            if (!sourceDirectory.Exists)
            {
                sourceDirectory.Create();
            }

            var resultDirectory = new DirectoryInfo(@"Images\KeepHNResults");
            if (!resultDirectory.Exists)
            {
                resultDirectory.Create();
            }
#endif

            var random = new Random();

            // Copy all files from Images\HN because the tags are going to be randomised
            foreach (var fileInfo in new DirectoryInfo(@"Images\HN").GetFiles())
            {
                var sourceImageFilePath = Path.Combine(sourceDirectory.FullName, fileInfo.Name);

                File.Copy(fileInfo.FullName, sourceImageFilePath, true);
            }

            var sourceDicomFiles = sourceDirectory.GetFiles().Select(f => DicomFile.Open(f.FullName, FileReadOption.ReadAll)).ToArray();

            DicomAnonymisationTests.AddRandomTags(random, sourceDicomFiles);

            var originalSlice = sourceDicomFiles.First();

            var testAETConfigModel = GetTestAETConfigModel();

            var receivePort = 160;

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultDirectory.FullName)))
            using (var deleteService = CreateDeleteService())
            using (var pushService = CreatePushService())
            using (var downloadService = CreateDownloadService(segmentationClient))
            using (var uploadService = CreateUploadService(segmentationClient))
            using (var receiveService = CreateReceiveService(receivePort))
            {
                // Start a DICOM receiver for the final DICOM-RT file
                var eventCount = new ConcurrentDictionary<DicomReceiveProgressCode, int>();
                var folderPath = string.Empty;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    folderPath = e.FolderPath;
                    eventCount.AddOrUpdate(e.ProgressCode, 1, (k, v) => v + 1);
                };

                StartDicomDataReceiver(dicomDataReceiver, testAETConfigModel.AETConfig.Destination.Port);

                // Start the services.
                deleteService.Start();
                pushService.Start();
                downloadService.Start();
                uploadService.Start();
                receiveService.Start();

                // Try a DICOM C-ECHO
                var dicomDataSender = new DicomDataSender();

                var echoResult = await dicomDataSender.DicomEchoAsync(
                    testAETConfigModel.CallingAET,
                    testAETConfigModel.CalledAET,
                    receivePort,
                    "127.0.0.1");
                Assert.IsTrue(echoResult == DicomOperationResult.Success);

                // Send the image stack
                DcmtkHelpers.SendFolderUsingDCMTK(
                    sourceDirectory.FullName,
                    receivePort,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: testAETConfigModel.CallingAET,
                    calledAETitle: testAETConfigModel.CalledAET);

                // Wait for DICOM-RT file to be received.
                Func<DicomReceiveProgressCode, int, bool> TestEventCount = (progressCode, count) =>
                    eventCount.ContainsKey(progressCode) && eventCount[progressCode] == count;

                SpinWait.SpinUntil(() => TestEventCount(DicomReceiveProgressCode.AssociationEstablished, 1));
                SpinWait.SpinUntil(() => TestEventCount(DicomReceiveProgressCode.FileReceived, 1));
                SpinWait.SpinUntil(() => TestEventCount(DicomReceiveProgressCode.AssociationReleased, 1));
                SpinWait.SpinUntil(() => TestEventCount(DicomReceiveProgressCode.ConnectionClosed, 1));

                Assert.IsTrue(eventCount[DicomReceiveProgressCode.AssociationEstablished] == 1);
                Assert.IsTrue(eventCount[DicomReceiveProgressCode.FileReceived] == 1);
                Assert.IsTrue(eventCount[DicomReceiveProgressCode.AssociationReleased] == 1);
                Assert.IsTrue(eventCount[DicomReceiveProgressCode.ConnectionClosed] == 1);

                Assert.IsFalse(string.IsNullOrWhiteSpace(folderPath));

                var receivedFiles = new DirectoryInfo(folderPath).GetFiles();
                Assert.AreEqual(1, receivedFiles.Length);

                var receivedFilePath = receivedFiles.First().FullName;

                var dicomFile = await DicomFile.OpenAsync(receivedFilePath, FileReadOption.ReadAll);

                Assert.IsNotNull(dicomFile);

                /*
                Assert.IsTrue(dicomFile.Dataset.GetString(DicomTag.SoftwareVersions).StartsWith("Microsoft InnerEye Gateway:"));
                Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 1).StartsWith("InnerEye AI Model:"));
                Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 2).StartsWith("InnerEye AI Model ID:"));
                Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 3).StartsWith("InnerEye Model Created:"));
                Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 4).StartsWith("InnerEye Version:"));

                Assert.AreEqual($"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month.ToString("D2")}{DateTime.UtcNow.Day.ToString("D2")}", dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesDate));
                Assert.IsTrue(dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty).StartsWith("1.2.826.0.1.3680043.2"));
                Assert.IsTrue(dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty).StartsWith("1.2.826.0.1.3680043.2"));
                */

                //DicomAnonymisationTests.VerifyDicomFile(receivedFilePath);

                var matchedModel = ApplyAETModelConfigProvider.ApplyAETModelConfig(testAETConfigModel.AETConfig.Config.ModelsConfig, sourceDicomFiles);

                DicomAnonymisationTests.AssertDeanonymizedFile(
                    originalSlice,
                    dicomFile,
                    segmentationClient,
                    matchedModel.Result.TagReplacements);
            }
        }
    }
}
