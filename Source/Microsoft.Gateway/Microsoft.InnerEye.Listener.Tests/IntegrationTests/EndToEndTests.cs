namespace Microsoft.InnerEye.Listener.Tests.IntegrationTests
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dicom;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
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
        [Timeout(IntegrationTestTimeout)]
        [Ignore("Integration test, relies on live API")]
        [Description("Pushes an entire DICOM Image Series.")]
        [TestMethod]
        public async Task IntegrationTestEndToEnd()
        {
            var segmentationClient = (IInnerEyeSegmentationClient)null;// GetMockInnerEyeSegmentationClient();

            var sourceDirectory = CreateTemporaryDirectory();

            var sampleSourceDicomFile = (DicomFile)null;

            var random = new Random();

            foreach (var sourceImageFileInfo in new DirectoryInfo(@"Images\HN").GetFiles())
            {
                var dicomFile = await DicomFile.OpenAsync(sourceImageFileInfo.FullName, FileReadOption.ReadAll);

                DicomAnonymisationTests.AddRandomTags(random, dicomFile);

                sampleSourceDicomFile = sampleSourceDicomFile ?? dicomFile;

                var sourceImageFilePath = Path.Combine(sourceDirectory.FullName, sourceImageFileInfo.Name);

                await dicomFile.SaveAsync(sourceImageFilePath);
            }

            var testAETConfigModel = GetTestAETConfigModel();

            var resultDirectory = CreateTemporaryDirectory();

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

                var originalSlice = sampleSourceDicomFile;

                Assert.IsNotNull(originalSlice);

                var receivedFiles = new DirectoryInfo(folderPath).GetFiles();
                Assert.AreEqual(1, receivedFiles.Length);

                var receivedFilePath = receivedFiles.First().FullName;

                var dicomFile = await DicomFile.OpenAsync(receivedFilePath, FileReadOption.ReadAll);

                Assert.IsNotNull(dicomFile);

                var constantStrings = new[]
                {
                    DicomTag.StudyDate,
                    DicomTag.AccessionNumber,
                    DicomTag.ReferringPhysicianName,
                    DicomTag.PatientName,
                    DicomTag.PatientID,
                    DicomTag.PatientBirthDate,
                    DicomTag.StudyInstanceUID,
                    DicomTag.StudyID
                };

                foreach (var constantString in constantStrings)
                {
                    //Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(constantString), dicomFile.Dataset.GetSingleValue<string>(constantString));
                }

                var constantOptionalStrings = new[]
                {
                    DicomTag.StudyDescription,
                };

                foreach (var constantOptionalString in constantOptionalStrings)
                {
                    //Assert.AreEqual(originalSlice.Dataset.GetSingleValueOrDefault(constantOptionalString, string.Empty), dicomFile.Dataset.GetSingleValueOrDefault(constantOptionalString, string.Empty));
                }

                var expectedStrings = new[]
                {
                    Tuple.Create("RTSTRUCT", DicomTag.Modality),
                    Tuple.Create("Microsoft Corporation", DicomTag.Manufacturer),
                    Tuple.Create("NOT FOR CLINICAL USE", DicomTag.SeriesDescription),
                    Tuple.Create("ANONYM", DicomTag.OperatorsName),
                    Tuple.Create("511091532", DicomTag.SeriesNumber),
                    Tuple.Create("1.2.840.10008.5.1.4.1.1.481.3", DicomTag.SOPClassUID),
                };

                foreach (var expectedString in expectedStrings)
                {
                    Assert.AreEqual(expectedString.Item1, dicomFile.Dataset.GetSingleValue<string>(expectedString.Item2));
                }

                Assert.IsTrue(dicomFile.Dataset.GetString(DicomTag.SoftwareVersions).StartsWith("Microsoft InnerEye Gateway:"));
                Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 1).StartsWith("InnerEye AI Model:"));
                Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 2).StartsWith("InnerEye AI Model ID:"));
                Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 3).StartsWith("InnerEye Model Created:"));
                Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 4).StartsWith("InnerEye Version:"));

                Assert.AreEqual($"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month.ToString("D2")}{DateTime.UtcNow.Day.ToString("D2")}", dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesDate));
                Assert.IsTrue(dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty).StartsWith("1.2.826.0.1.3680043.2"));
                Assert.IsTrue(dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty).StartsWith("1.2.826.0.1.3680043.2"));

                DicomAnonymisationTests.VerifyDicomFile(receivedFilePath);
            }
        }
    }
}
