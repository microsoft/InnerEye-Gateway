namespace Microsoft.InnerEye.Integration.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Security.Authentication;
    using System.Threading;
    using System.Threading.Tasks;
    using Dicom;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Models;
    using Microsoft.InnerEye.Listener.Tests;
    using Microsoft.InnerEye.Listener.Tests.Common.Helpers;
    using Microsoft.InnerEye.Listener.Tests.ServiceTests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EndToEndTests : BaseTestClass
    {
        /// <summary>
        /// 40 minute timeout for end-to-end test.
        /// </summary>
        public const int IntegrationTestTimeout = 40 * 60 * 1000;

        /// <summary>
        /// Default list of class names used by the PassThroughModel.
        /// </summary>
        public static readonly string[] PassThroughModelDisplayNames = new[] { "SpinalCord", "Lung_R", "Lung_L", "Heart", "Esophagus" };

        [TestCategory("IntegrationTests")]
        [Timeout(IntegrationTestTimeout)]
        [Description("Tests test configuration of inference service URI and license key.")]
        [TestMethod]
        public async Task IntegrationTestLicenseKey()
        {
            try
            {
                using (var segmentationClient = TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient()())
                {
                    await segmentationClient.PingAsync().ConfigureAwait(false);
                }
            }
            catch (AuthenticationException)
            {
                Assert.Fail("Invalid product key. Check ProcessorSettings.LicenseKeyEnvVar in TestConfigurations/GatewayProcessorConfig.json and the corresponding system environment variable.");
            }
            catch (InvalidOperationException)
            {
                Assert.Fail("Unable to connect to inference service uri. Check ProcessorSettings.InferenceUri in TestConfigurations/GatewayProcessorConfig.json.");
            }
        }

        [TestCategory("IntegrationTests")]
        [Timeout(IntegrationTestTimeout)]
        [Description("Pushes an entire DICOM Image Series.")]
        [TestMethod]
        public async Task IntegrationTestEndToEnd()
        {
            var sourceDirectory = CreateTemporaryDirectory();
            var resultDirectory = CreateTemporaryDirectory();

            var random = new Random();

            // Get file names for all in directory
            var sourceDicomFileNames = new DirectoryInfo(@"Images\HN").GetFiles().ToArray();
            // Load all DICOM files
            var sourceDicomFiles = sourceDicomFileNames.Select(f => DicomFile.Open(f.FullName, FileReadOption.ReadAll)).ToArray();

            // Add/Update random tags for the source DICOM files.
            DicomAnonymisationTests.AddRandomTags(random, sourceDicomFiles);

            // Save them all to the sourceDirectory.
            var sourcePairs = sourceDicomFileNames.Zip(sourceDicomFiles, (f, d) => Tuple.Create(f, d)).ToArray();
            foreach (var sourcePair in sourcePairs)
            {
                var sourceImageFilePath = Path.Combine(sourceDirectory.FullName, sourcePair.Item1.Name);

                sourcePair.Item2.Save(sourceImageFilePath);
            }

            // Keep the first as a reference for deanonymization later.
            var originalSlice = sourceDicomFiles.First();

            var testAETConfigModel = GetTestAETConfigModel();

            var receivePort = 160;

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultDirectory.FullName)))
            using (var deleteService = CreateDeleteService())
            using (var pushService = CreatePushService())
            using (var downloadService = CreateDownloadService())
            using (var uploadService = CreateUploadService())
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
                    "127.0.0.1").ConfigureAwait(false);
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

                var receivedFiles = new DirectoryInfo(folderPath).GetFiles();
                Assert.AreEqual(1, receivedFiles.Length);

                var receivedFilePath = receivedFiles.First().FullName;

                var dicomFile = await DicomFile.OpenAsync(receivedFilePath, FileReadOption.ReadAll).ConfigureAwait(false);

                Assert.IsNotNull(dicomFile);

                var matchedModel = ApplyAETModelConfigProvider.ApplyAETModelConfig(testAETConfigModel.AETConfig.Config.ModelsConfig, sourceDicomFiles);

                var segmentationClient = TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient().Invoke();

                DicomAnonymisationTests.AssertDeanonymizedFile(
                    originalSlice,
                    dicomFile,
                    segmentationClient.TopLevelReplacements,
                    matchedModel.Result.TagReplacements,
                    false);

                AssertIsDicomRtFile(DateTime.UtcNow, dicomFile, matchedModel.Result.ModelId);
            }
        }

        /// <summary>
        /// Assert that DicomFile is a DICOM-RT file.
        /// </summary>
        /// <param name="testDateTime">Time of test.</param>
        /// <param name="dicomFile">DicomFile to test.</param>
        /// <param name="modelId">Expected modelId.</param>
        public static void AssertIsDicomRtFile(DateTime testDateTime, DicomFile dicomFile, string modelId)
        {
            dicomFile = dicomFile ?? throw new ArgumentNullException(nameof(dicomFile));

            Assert.AreEqual(DicomUID.RTStructureSetStorage, dicomFile.FileMetaInfo.MediaStorageSOPClassUID);
            var sopInstanceUID = dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID;
            Assert.AreEqual(DicomTransferSyntax.ImplicitVRLittleEndian, dicomFile.FileMetaInfo.TransferSyntax);

            Assert.AreEqual(DicomUID.RTStructureSetStorage, dicomFile.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID));
            Assert.AreEqual(sopInstanceUID, dicomFile.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPInstanceUID));

            var expectedDate = $"{testDateTime.Year}{testDateTime.Month:D2}{testDateTime.Day:D2}";
            Assert.AreEqual(expectedDate, dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesDate));
            Assert.AreEqual("RTSTRUCT", dicomFile.Dataset.GetSingleValue<string>(DicomTag.Modality));
            Assert.AreEqual("Microsoft Corporation", dicomFile.Dataset.GetSingleValue<string>(DicomTag.Manufacturer));
            Assert.AreEqual("NOT FOR CLINICAL USE", dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesDescription));
            Assert.AreEqual(string.Empty, dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.OperatorsName, string.Empty));

            Assert.IsTrue(dicomFile.Dataset.GetString(DicomTag.SoftwareVersions).StartsWith("Microsoft InnerEye Gateway:", StringComparison.Ordinal));
            Assert.AreEqual(modelId, dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 1));

            Assert.AreEqual(string.Empty, dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesNumber, string.Empty));

            Assert.AreEqual("InnerEye", dicomFile.Dataset.GetSingleValue<string>(DicomTag.StructureSetLabel));
            Assert.AreEqual("NOT FOR CLINICAL USE", dicomFile.Dataset.GetSingleValue<string>(DicomTag.StructureSetName));
            Assert.AreEqual(string.Empty, dicomFile.Dataset.GetSingleValue<string>(DicomTag.StructureSetDescription));
            Assert.AreEqual(expectedDate, dicomFile.Dataset.GetSingleValue<string>(DicomTag.StructureSetDate));

            var structureSetROISequences = dicomFile.Dataset.GetSequence(DicomTag.StructureSetROISequence);
            Assert.AreEqual(PassThroughModelDisplayNames.Length, structureSetROISequences.Items.Count);

            var roiContourSequences = dicomFile.Dataset.GetSequence(DicomTag.ROIContourSequence);
            Assert.AreEqual(PassThroughModelDisplayNames.Length, roiContourSequences.Items.Count);

            var rtRoiObservationsSequence = dicomFile.Dataset.GetSequence(DicomTag.RTROIObservationsSequence);
            Assert.AreEqual(PassThroughModelDisplayNames.Length, rtRoiObservationsSequence.Items.Count);

            for (var i = 0; i < PassThroughModelDisplayNames.Length; i++)
            {
                Assert.AreEqual(i + 1, structureSetROISequences.Items[i].GetSingleValue<int>(DicomTag.ROINumber));

                var expectedName = PassThroughModelDisplayNames[i] + " NOT FOR CLINICAL USE";
                Assert.AreEqual(expectedName, structureSetROISequences.Items[i].GetSingleValue<string>(DicomTag.ROIName));

                Assert.AreEqual(i + 1, roiContourSequences.Items[i].GetSingleValue<int>(DicomTag.ReferencedROINumber));

                Assert.AreEqual(i, rtRoiObservationsSequence.Items[i].GetSingleValue<int>(DicomTag.ObservationNumber));
                Assert.AreEqual(i + 1, rtRoiObservationsSequence.Items[i].GetSingleValue<int>(DicomTag.ReferencedROINumber));
                Assert.AreEqual("ORGAN", rtRoiObservationsSequence.Items[i].GetSingleValue<string>(DicomTag.RTROIInterpretedType));
            }
        }
    }
}
