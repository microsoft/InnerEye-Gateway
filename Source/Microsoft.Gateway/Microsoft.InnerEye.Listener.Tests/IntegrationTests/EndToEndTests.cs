namespace Microsoft.InnerEye.Listener.Tests.IntegrationTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dicom;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Models;
    using Microsoft.InnerEye.Listener.Tests.Common.Helpers;
    using Microsoft.InnerEye.Listener.Tests.Models;
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
        // [Ignore("Integration test, relies on live API")]
        [Description("Pushes an entire DICOM Image Series.")]
        [TestMethod]
        public async Task IntegrationTestEndToEnd()
        {
            var segmentationClient = GetMockInnerEyeSegmentationClient();

            var sourceDirectory = new DirectoryInfo(@"Images\Temp");// CreateTemporaryDirectory();
            if (!sourceDirectory.Exists)
            {
                sourceDirectory.Create();
            }

            var sampleSourceDicomFile = (DicomFile)null;

            foreach (var sourceImageFileInfo in new DirectoryInfo(@"Images\HN").GetFiles())
            {
                var dicomFile = await DicomFile.OpenAsync(sourceImageFileInfo.FullName);

                AddRandomTags(dicomFile);

                sampleSourceDicomFile = sampleSourceDicomFile ?? dicomFile;

                var destinationImageFile = Path.Combine(sourceDirectory.FullName, sourceImageFileInfo.Name);

                await dicomFile.SaveAsync(destinationImageFile);
            }

            var testAETConfigModel = GetTestAETConfigModel();

            var aetConfigProvider = new MockAETConfigProvider(testAETConfigModel);

            var resultDirectory = CreateTemporaryDirectory();

            var receivePort = 160;

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultDirectory.FullName)))
            using (var deleteService = CreateDeleteService())
            using (var pushService = CreatePushService(aetConfigProvider.AETConfigModels))
            using (var downloadService = CreateDownloadService(segmentationClient))
            using (var uploadService = CreateUploadService(segmentationClient, aetConfigProvider.AETConfigModels))
            using (var receiveService = CreateReceiveService(receivePort))
            {
                // Start a DICOM receiver for the final DICOM-RT file
                var eventCount = new ConcurrentDictionary<DicomReceiveProgressCode, int>();
                var folderPath = string.Empty;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    folderPath = e.FolderPath;
                    eventCount.AddOrUpdate(e.ProgressCode, 1, (k, v) => v + 1);
                    Debug.WriteLine(string.Format("****** {0}", e.ProgressCode));
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

        /// <summary>
        /// Add some random tags.
        /// </summary>
        /// <param name="dicomFile">Dicom file to update.</param>
        public static void AddRandomTags(DicomFile dicomFile)
        {
            var random = new Random();
            var dataSet = dicomFile.Dataset;

            foreach (var dicomTagRandomiser in DicomTagRandomisers)
            {
                dataSet.AddOrUpdate(dicomTagRandomiser.Item2.Invoke(dicomTagRandomiser.Item1, random));
            }
        }

        /// <summary>
        /// Func to create a random RandomDicomDate.
        /// </summary>
        private static readonly Func<DicomTag, Random, DicomItem> RandomDicomDate = (tag, random) =>
            new DicomDate(tag, DateTime.UtcNow.AddDays(random.NextDouble() * 1000.0));

        /// <summary>
        /// Func to create a random DicomAgeString.
        /// </summary>
        private static readonly Func<DicomTag, Random, DicomItem> RandomDicomAgeString = (tag, random) =>
            new DicomAgeString(tag, string.Format("{0:D3}Y", random.Next(18, 100)));

        /// <summary>
        /// Func to create a random DicomPersonName.
        /// </summary>
        private static readonly Func<DicomTag, Random, DicomItem> RandomDicomPersonName = (tag, random) =>
            new DicomPersonName(tag, ConfigurationProviderTests.RandomString(random, 9));

        /// <summary>
        /// Func to create a random RandomDicomShortString.
        /// </summary>
        private static readonly Func<DicomTag, Random, DicomItem> RandomDicomShortString = (tag, random) =>
            new DicomShortString(tag, ConfigurationProviderTests.RandomString(random, 12));

        /// <summary>
        /// Func to create a random RandomDicomShortText.
        /// </summary>
        private static readonly Func<DicomTag, Random, DicomItem> RandomDicomShortText = (tag, random) =>
            new DicomShortText(tag, ConfigurationProviderTests.RandomString(random, 33));

        /// <summary>
        /// Func to create a random RandomDicomLongString.
        /// </summary>
        private static readonly Func<DicomTag, Random, DicomItem> RandomDicomLongString = (tag, random) =>
            new DicomLongString(tag, ConfigurationProviderTests.RandomString(random, 18));

        /// <summary>
        /// Func to create a random RandomDicomLongText.
        /// </summary>
        private static readonly Func<DicomTag, Random, DicomItem> RandomDicomLongText = (tag, random) =>
            new DicomLongText(tag, ConfigurationProviderTests.RandomString(random, 65));

        /// <summary>
        /// Func to create a random RandomDicomTime.
        /// </summary>
        private static readonly Func<DicomTag, Random, DicomItem> RandomDicomTime = (tag, random) =>
            new DicomTime(tag, DateTime.UtcNow.AddSeconds(random.NextDouble() * 1000.0));

        /// <summary>
        /// List of DicomTags to randomise and a function to use to do the randomisation.
        /// </summary>
        private static readonly Tuple<DicomTag, Func<DicomTag, Random, DicomItem>>[] DicomTagRandomisers = new[]
        {
            Tuple.Create(DicomTag.PatientAge, RandomDicomAgeString),

            Tuple.Create(DicomTag.OperatorsName, RandomDicomPersonName),
            Tuple.Create(DicomTag.PatientName, RandomDicomPersonName),
            Tuple.Create(DicomTag.PerformingPhysicianName, RandomDicomPersonName),
            Tuple.Create(DicomTag.PhysiciansOfRecord, RandomDicomPersonName),
            Tuple.Create(DicomTag.ReferringPhysicianName, RandomDicomPersonName),

            //Tuple.Create(DicomTag.ImplementationVersionName, RandomDicomShortString),
            Tuple.Create(DicomTag.AccessionNumber, RandomDicomShortString),
            Tuple.Create(DicomTag.StationName, RandomDicomShortString),
            Tuple.Create(DicomTag.StudyID, RandomDicomShortString),

            Tuple.Create(DicomTag.Manufacturer, RandomDicomLongString),
            Tuple.Create(DicomTag.InstitutionName, RandomDicomLongString),
            Tuple.Create(DicomTag.StudyDescription, RandomDicomLongString),
            Tuple.Create(DicomTag.SeriesDescription, RandomDicomLongString),
            Tuple.Create(DicomTag.InstitutionalDepartmentName, RandomDicomLongString),
            Tuple.Create(DicomTag.ManufacturerModelName, RandomDicomLongString),
            Tuple.Create(DicomTag.PatientID, RandomDicomLongString),
            Tuple.Create(DicomTag.IssuerOfPatientID, RandomDicomLongString),
            Tuple.Create(DicomTag.PatientAddress, RandomDicomLongString),
            Tuple.Create(DicomTag.SoftwareVersions, RandomDicomLongString),

            Tuple.Create(DicomTag.InstitutionAddress, RandomDicomShortText),

            Tuple.Create(DicomTag.AdditionalPatientHistory, RandomDicomLongText),
            Tuple.Create(DicomTag.PatientComments, RandomDicomLongText),

            Tuple.Create(DicomTag.InstanceCreationDate, RandomDicomDate),
            Tuple.Create(DicomTag.StudyDate, RandomDicomDate),
            Tuple.Create(DicomTag.SeriesDate, RandomDicomDate),
            Tuple.Create(DicomTag.AcquisitionDate, RandomDicomDate),
            Tuple.Create(DicomTag.ContentDate, RandomDicomDate),
            Tuple.Create(DicomTag.PatientBirthDate, RandomDicomDate),

            Tuple.Create(DicomTag.InstanceCreationTime, RandomDicomTime),
            Tuple.Create(DicomTag.StudyTime, RandomDicomTime),
            Tuple.Create(DicomTag.SeriesTime, RandomDicomTime),
            Tuple.Create(DicomTag.SeriesTime, RandomDicomTime),
        };
    }
}
