﻿namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;
    using Microsoft.InnerEye.Listener.Common;
    using Microsoft.InnerEye.Listener.Tests.Common.Helpers;
    using Microsoft.InnerEye.Listener.Tests.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DicomAnonymisationTests : BaseTestClass
    {
        [TestCategory("DicomAnonymisationDCMTK")]
        [Timeout(180 * 1000)]
        [Description("Pushes an entire CT Image Series using the dry-run model de-anonymisation configuration. " +
                     "Checks the result structure set is correctly de-anonymised and contains the Gateway version number. " +
                     "This test also uses dciodvfy to verify the result Dicom file is a valid file.")]
        [TestMethod]
        public async Task GenerateAndTestDeAnonymisedStructureSetFile()
        {
            var image = @"Images\LargeSeriesWithContour";
            var tempFolder = CreateTemporaryDirectory();

            var segmentationClient = GetMockInnerEyeSegmentationClient();

            var configType = AETConfigType.ModelWithResultDryRun;
            var dryRunFolder = DryRunFolders.GetFolder(configType);

            var testAETConfigModel = GetTestAETConfigModel();

            var newTestAETConfigModel = testAETConfigModel.With(
                aetConfig: new ClientAETConfig(
                    new AETConfig(
                        configType,
                        testAETConfigModel.AETConfig.Config.ModelsConfig),
                    testAETConfigModel.AETConfig.Destination,
                    false));

            var aetConfigProvider = new MockAETConfigProvider(newTestAETConfigModel);

            var receivePort = 160;

            using (var deleteService = CreateDeleteService())
            using (var pushService = CreatePushService(aetConfigProvider.AETConfigModels))
            using (var downloadService = CreateDownloadService(segmentationClient))
            using (var uploadService = CreateUploadService(segmentationClient, aetConfigProvider.AETConfigModels))
            using (var receiveService = CreateReceiveService(receivePort, tempFolder))
            {
                deleteService.Start();
                pushService.Start();
                downloadService.Start();
                uploadService.Start();
                receiveService.Start();

                DcmtkHelpers.SendFolderUsingDCMTK(
                    image,
                    receivePort,
                    ScuProfile.LEExplicitCT,
                    TestContext,
                    applicationEntityTitle: newTestAETConfigModel.CallingAET,
                    calledAETitle: newTestAETConfigModel.CalledAET);

                SpinWait.SpinUntil(() => tempFolder.GetDirectories().FirstOrDefault(x => x.FullName.Contains(dryRunFolder)) != null);

                var dryRunFolderDirectory = tempFolder.GetDirectories().First(x => x.FullName.Contains(dryRunFolder)).GetDirectories().First();

                // Wait until we have all image files.
                SpinWait.SpinUntil(() => dryRunFolderDirectory.GetFiles().Length == 1);

                // Wait for all files to save.
                await Task.Delay(1000);

                var originalSlice = DicomFile.Open(new DirectoryInfo(image).GetFiles().First().FullName);

                Assert.IsNotNull(originalSlice);

                foreach (var file in dryRunFolderDirectory.GetFiles())
                {
                    var dicomFile = DicomFile.Open(file.FullName, FileReadOption.ReadAll);

                    Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(DicomTag.StudyDate), dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyDate));
                    Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber), dicomFile.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber));
                    Assert.AreEqual("RTSTRUCT", dicomFile.Dataset.GetSingleValue<string>(DicomTag.Modality));
                    Assert.AreEqual("Microsoft Corporation", dicomFile.Dataset.GetSingleValue<string>(DicomTag.Manufacturer));
                    Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(DicomTag.ReferringPhysicianName), dicomFile.Dataset.GetSingleValue<string>(DicomTag.ReferringPhysicianName));
                    Assert.AreEqual(originalSlice.Dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty), dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty));
                    Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(DicomTag.PatientName), dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName));
                    Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(DicomTag.PatientID), dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientID));
                    Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(DicomTag.PatientBirthDate), dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientBirthDate));
                    Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID), dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
                    Assert.AreEqual(originalSlice.Dataset.GetSingleValue<string>(DicomTag.StudyID), dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyID));

                    Assert.IsTrue(dicomFile.Dataset.GetString(DicomTag.SoftwareVersions).StartsWith("Microsoft InnerEye Gateway:"));
                    Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 1).StartsWith("InnerEye AI Model:"));
                    Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 2).StartsWith("InnerEye AI Model ID:"));
                    Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 3).StartsWith("InnerEye Model Created:"));
                    Assert.IsTrue(dicomFile.Dataset.GetValue<string>(DicomTag.SoftwareVersions, 4).StartsWith("InnerEye Version:"));

                    Assert.AreEqual("1.2.840.10008.5.1.4.1.1.481.3", dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID));
                    Assert.AreEqual($"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month.ToString("D2")}{DateTime.UtcNow.Day.ToString("D2")}", dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesDate));
                    Assert.IsTrue(dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty).StartsWith("1.2.826.0.1.3680043.2"));
                    Assert.AreEqual("511091532", dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesNumber, string.Empty));
                    Assert.AreEqual("NOT FOR CLINICAL USE", dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesDescription, string.Empty));
                    Assert.IsTrue(dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty).StartsWith("1.2.826.0.1.3680043.2"));
                    Assert.AreEqual("ANONYM", dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.OperatorsName, string.Empty));

                    VerifyDicomFile(file.FullName);
                }
            }
        }

        [TestCategory("DicomAnonymisationDCMTK")]
        [Timeout(60 * 1000)]
        [Description("Pushes an entire CT Image Series using the dry-run model configuration. " +
                     "Checks every Dicom slice in the series is correctly anonymised and contains the Gateway version number. " +
                     "This test also generates a sample anonymised CT Slice.")]
        [TestMethod]
        public async Task GenerateAndTestAnonymisedDicomCTFile()
        {
            var tempFolder = CreateTemporaryDirectory();

            {
                var configType = AETConfigType.ModelDryRun;
                var dryRunFolder = DryRunFolders.GetFolder(configType);

                var segmentationClient = GetMockInnerEyeSegmentationClient();

                var testAETConfigModel = GetTestAETConfigModel();
                var newTestAETConfigModel = testAETConfigModel.With(
                    aetConfig: new ClientAETConfig(
                        new AETConfig(
                            configType,
                            null),
                        testAETConfigModel.AETConfig.Destination,
                        false));

                var aetConfigProvider = new MockAETConfigProvider(newTestAETConfigModel);

                var receivePort = 161;

                using (var deleteService = CreateDeleteService())
                using (var pushService = CreatePushService(aetConfigProvider.AETConfigModels))
                using (var downloadService = CreateDownloadService(segmentationClient))
                using (var uploadService = CreateUploadService(segmentationClient, aetConfigProvider.AETConfigModels))
                using (var receiveService = CreateReceiveService(receivePort, tempFolder))
                {
                    deleteService.Start();
                    pushService.Start();
                    downloadService.Start();
                    uploadService.Start();
                    receiveService.Start();

                    DcmtkHelpers.SendFolderUsingDCMTK(
                        @"Images\1ValidSmall",
                        receivePort,
                        ScuProfile.LEExplicitCT,
                        TestContext,
                        applicationEntityTitle: newTestAETConfigModel.CallingAET,
                        calledAETitle: newTestAETConfigModel.CalledAET);

                    SpinWait.SpinUntil(() => tempFolder.GetDirectories().FirstOrDefault(x => x.FullName.Contains(dryRunFolder)) != null);

                    var dryRunFolderDirectory = tempFolder.GetDirectories().First(x => x.FullName.Contains(dryRunFolder)).GetDirectories().First();

                    // Wait until we have all image files.
                    SpinWait.SpinUntil(() => dryRunFolderDirectory.GetFiles().Length == 20);

                    // Wait for all files to save.
                    await Task.Delay(200);

                    var savedSampleFile = false;

                    foreach (var file in dryRunFolderDirectory.GetFiles())
                    {
                        var dicomFile = DicomFile.Open(file.FullName);

                        AssertDicomFileIsAnonymised(dicomFile);

                        if (!savedSampleFile)
                        {
                            WriteDicomFileForBuildPackage("AnonymisedCT.dcm", dicomFile);
                            savedSampleFile = true;
                        }
                    }
                }
            }
        }

        [TestCategory("DicomAnonymisationDCMTK")]
        [Timeout(60 * 1000)]
        [Description("Pushes an entire RT structure set file and an CT Series using the dry-run feedback configuration. " +
                     "Checks the anonymised RT structure set file is correctly anonymised and contains the Gateway version number. " +
                     "This test also generates a sample Anonymised RT Structure Set.")]
        [TestMethod]
        public async Task GenerateAndTestAnonymisedRTFile()
        {
            var tempFolder = CreateTemporaryDirectory();

            {
                var configType = AETConfigType.ModelDryRun;
                var dryRunFolder = DryRunFolders.GetFolder(configType);

                var segmentationClient = GetMockInnerEyeSegmentationClient();

                var testAETConfigModel = GetTestAETConfigModel();
                var newTestAETConfigModel = testAETConfigModel.With(
                    aetConfig: new ClientAETConfig(
                        new AETConfig(
                            configType,
                            null),
                        testAETConfigModel.AETConfig.Destination,
                        false));

                var aetConfigProvider = new MockAETConfigProvider(newTestAETConfigModel);

                var receivePort = 162;

                using (var deleteService = CreateDeleteService())
                using (var pushService = CreatePushService(aetConfigProvider.AETConfigModels))
                using (var downloadService = CreateDownloadService(segmentationClient))
                using (var uploadService = CreateUploadService(segmentationClient, aetConfigProvider.AETConfigModels))
                using (var receiveService = CreateReceiveService(receivePort, tempFolder))
                {
                    deleteService.Start();
                    pushService.Start();
                    downloadService.Start();
                    uploadService.Start();
                    receiveService.Start();

                    DcmtkHelpers.SendFolderUsingDCMTK(
                        @"Images\1ValidSmall",
                        receivePort,
                        ScuProfile.LEExplicitRTCT,
                        TestContext,
                        applicationEntityTitle: newTestAETConfigModel.CallingAET,
                        calledAETitle: newTestAETConfigModel.CalledAET);

                    SpinWait.SpinUntil(() => tempFolder.GetDirectories().FirstOrDefault(x => x.FullName.Contains(dryRunFolder)) != null);

                    var dryRunFolderDirectory = tempFolder.GetDirectories().First(x => x.FullName.Contains(dryRunFolder)).GetDirectories().First();

                    // Wait until we have all image files.
                    SpinWait.SpinUntil(() => dryRunFolderDirectory.GetFiles().Length == 1);

                    // Wait for all files to save.
                    await Task.Delay(1000);

                    var savedSampleFile = false;

                    foreach (var file in dryRunFolderDirectory.GetFiles())
                    {
                        var dicomFile = DicomFile.Open(file.FullName);

                        AssertDicomFileIsAnonymised(dicomFile);

                        if (!savedSampleFile)
                        {
                            WriteDicomFileForBuildPackage("AnonymisedRT.dcm", dicomFile);
                            savedSampleFile = true;
                        }
                    }
                }
            }
        }

        public static void VerifyDicomFile(string path)
        {
            var verifierPath = Path.Combine("Assets", "dicom3tools", "dciodvfy.exe");
            Assert.IsNotNull(verifierPath, "DICOM verifier executable (dciodvfy.exe) not found on system PATH");

            var process = new Process();
            process.StartInfo.FileName = verifierPath;
            process.StartInfo.Arguments = path;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Ignore empty contours
            var output = standardError
                .Replace("Error - Bad Sequence number of Items 0 (1-n Required by Module definition) Element=<ContourSequence> Module=<ROIContour>", string.Empty)
                .Replace("Error - Bad attribute Value Multiplicity Type 3 Optional Element=<ContourSequence> Module=<ROIContour>", string.Empty)
                .ToLower();

            Assert.IsFalse(output.Contains("error"));
        }

        public static void AssertDicomFileIsAnonymised(DicomFile dicomFile)
        {
            // Check the software version gets added
            var softwareVersion = dicomFile.Dataset.GetString(DicomTag.SoftwareVersions);

            Assert.IsTrue(softwareVersion.StartsWith("Microsoft InnerEye Gateway:"));

            var acceptedTags = new List<DicomTag>()
            {
                DicomTag.ImageType,
                DicomTag.SOPClassUID,
                DicomTag.SOPInstanceUID,
                DicomTag.Modality,
                DicomTag.PatientID,
                DicomTag.BodyPartExamined,
                DicomTag.SoftwareVersions,
                DicomTag.PatientPosition,
                DicomTag.StudyInstanceUID,
                DicomTag.SeriesInstanceUID,
                DicomTag.ImagePositionPatient,
                DicomTag.ImageOrientationPatient,
                DicomTag.FrameOfReferenceUID,
                DicomTag.SliceLocation,
                DicomTag.SamplesPerPixel,
                DicomTag.PhotometricInterpretation,
                DicomTag.Rows,
                DicomTag.Columns,
                DicomTag.PixelSpacing,
                DicomTag.BitsAllocated,
                DicomTag.BitsStored,
                DicomTag.HighBit,
                DicomTag.PixelRepresentation,
                DicomTag.RescaleIntercept,
                DicomTag.RescaleSlope,
                DicomTag.PixelData,

                // Structure set tags
                DicomTag.StructureSetLabel,
                DicomTag.StructureSetName,
                DicomTag.ReferencedFrameOfReferenceSequence,
                DicomTag.StructureSetROISequence,
                DicomTag.ROIContourSequence,
                DicomTag.RTROIObservationsSequence,

                // Deidentification
                DicomTag.DeidentificationMethod,
                DicomTag.PatientIdentityRemoved,
                DicomTag.LongitudinalTemporalInformationModified,
            };

            var tagsInDataset = dicomFile.Dataset.Select(x => x.Tag).ToList();

            Assert.IsTrue(tagsInDataset.Count <= 29);

            foreach (var item in tagsInDataset)
            {
                // Check that this is an accepted tag
                Assert.IsTrue(acceptedTags.Contains(item), $"The Dicom file contained the Tag: {item.DictionaryEntry.Name}");
            }
        }


        /// <summary>
        /// List of DicomTags to randomise with RandomDicomAgeString.
        /// </summary>
        private static readonly DicomTag[] DicomAgeStringTagRandomisers = new[]
        {
            DicomTag.PatientAge,
        };

        /// <summary>
        /// List of DicomTags to randomise with RandomDicomPersonName.
        /// </summary>
        private static readonly DicomTag[] DicomPersonNameTagRandomisers = new[]
        {
            DicomTag.OperatorsName,
            DicomTag.PatientName,
            DicomTag.PerformingPhysicianName,
            DicomTag.PhysiciansOfRecord,
            DicomTag.ReferringPhysicianName,
        };

        /// <summary>
        /// List of DicomTags to randomise with RandomDicomShortString.
        /// </summary>
        private static readonly DicomTag[] DicomShortStringTagRandomisers = new[]
        {
            //DicomTag.ImplementationVersionName,
            DicomTag.AccessionNumber,
            DicomTag.StationName,
            DicomTag.StudyID,
        };

        /// <summary>
        /// List of DicomTags to randomise with RandomDicomLongString.
        /// </summary>
        private static readonly DicomTag[] DicomLongStringTagRandomisers = new[]
        {
            DicomTag.Manufacturer,
            DicomTag.InstitutionName,
            DicomTag.StudyDescription,
            DicomTag.SeriesDescription,
            DicomTag.InstitutionalDepartmentName,
            DicomTag.ManufacturerModelName,
            DicomTag.PatientID,
            DicomTag.IssuerOfPatientID,
            DicomTag.PatientAddress,
            DicomTag.SoftwareVersions,
        };

        /// <summary>
        /// List of DicomTags to randomise with RandomDicomShortText.
        /// </summary>
        private static readonly DicomTag[] DicomShortTextTagRandomisers = new[]
        {
            DicomTag.InstitutionAddress,
        };

        /// <summary>
        /// List of DicomTags to randomise with RandomDicomLongText.
        /// </summary>
        private static readonly DicomTag[] DicomLongTextTagRandomisers = new[]
        {
            DicomTag.AdditionalPatientHistory,
            DicomTag.PatientComments,
        };

        /// <summary>
        /// List of DicomTags to randomise with RandomDicomDate.
        /// </summary>
        private static readonly DicomTag[] DicomDateTagRandomisers = new[]
        {
            DicomTag.InstanceCreationDate,
            DicomTag.StudyDate,
            DicomTag.SeriesDate,
            DicomTag.AcquisitionDate,
            DicomTag.ContentDate,
            DicomTag.PatientBirthDate,
        };

        /// <summary>
        /// List of DicomTags to randomise with RandomDicomTime.
        /// </summary>
        private static readonly DicomTag[] DicomTimeTagRandomisers = new[]
        {
            DicomTag.InstanceCreationTime,
            DicomTag.StudyTime,
            DicomTag.SeriesTime,
            DicomTag.SeriesTime,
        };

        /// <summary>
        /// List of DicomTags to randomise with RandomDicomPatientSexCodeString.
        /// </summary>
        private static readonly DicomTag[] DicomSexCodeStringTagRandomisers = new[]
        {
            DicomTag.PatientSex,
        };

        /// <summary>
        /// List of DicomTags to randomise and a function to use to do the randomisation.
        /// </summary>
        public static readonly Tuple<DicomTag[], Func<DicomTag, Random, DicomItem>>[] DicomTagRandomisers = new[]
        {
            Tuple.Create(DicomAgeStringTagRandomisers, RandomDicomAgeString),
            Tuple.Create(DicomPersonNameTagRandomisers, RandomDicomPersonName),
            Tuple.Create(DicomShortStringTagRandomisers, RandomDicomShortString),
            Tuple.Create(DicomLongStringTagRandomisers, RandomDicomLongString),
            Tuple.Create(DicomShortTextTagRandomisers, RandomDicomShortText),
            Tuple.Create(DicomLongTextTagRandomisers, RandomDicomLongText),
            Tuple.Create(DicomDateTagRandomisers, RandomDicomDate),
            Tuple.Create(DicomTimeTagRandomisers, RandomDicomTime),
            Tuple.Create(DicomSexCodeStringTagRandomisers, RandomDicomPatientSexCodeString),
        };

        /// <summary>
        /// Add some random tags.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="dicomFile">Dicom file to update.</param>
        public static void AddRandomTags(Random random, DicomFile dicomFile)
        {
            var dataSet = dicomFile.Dataset;

            foreach (var dicomTagRandomiserPair in DicomTagRandomisers)
            {
                foreach (var dicomTag in dicomTagRandomiserPair.Item1)
                {
                    dataSet.AddOrUpdate(dicomTagRandomiserPair.Item2.Invoke(dicomTag, random));
                }
            }
        }

        [TestCategory("DicomAnonymisationDCMTK")]
        [Description("Check data sets can be anonymised/de-anonymised, just the top level replacements.")]
        [TestMethod]
        public async Task TestDataSetAnonymiseDeanonymizeTopLevelReplacements()
        {
            var random = new Random();

            var sourceImageFileInfo = new DirectoryInfo(@"Images\HN").GetFiles().First();

            var originalDicomFile = await DicomFile.OpenAsync(sourceImageFileInfo.FullName, FileReadOption.ReadAll);
            // Make a copy of the existing DicomDataset
            var originalDataset = originalDicomFile.Dataset.Clone();

            AddRandomTags(random, originalDicomFile);
            var sourceDataset = originalDicomFile.Dataset;

            // Check that the randomisation of the DicomDataset has actually changed the tags
            foreach (var dicomTagRandomiserPair in DicomTagRandomisers)
            {
                foreach (var dicomTag in dicomTagRandomiserPair.Item1)
                {
                    // Value may not even exist in the original dataset
                    var originalValue = originalDataset.GetSingleValueOrDefault(dicomTag, string.Empty);
                    // But should always exist after randomisation
                    var sourceValue = sourceDataset.GetSingleValue<string>(dicomTag);

                    // Check they are different, as strings.
                    Assert.IsFalse(string.IsNullOrEmpty(sourceValue));
                    Assert.AreNotEqual(originalValue, sourceValue);
                }
            }

            var innerEyeSegmentationClient = TestGatewayProcessorConfigProvider.CreateInnerEyeSegmentationClient()();

            // Anonymize the original DICOM file
            var anonymizedDicomFile = innerEyeSegmentationClient.AnonymizeDicomFile(originalDicomFile, innerEyeSegmentationClient.SegmentationAnonymisationProtocolId, innerEyeSegmentationClient.SegmentationAnonymisationProtocol);

            anonymizedDicomFile.Dataset.AddOrUpdate(DicomTag.SoftwareVersions, "Microsoft InnerEye Gateway:");

            // Check it has been anonymized
            AssertDicomFileIsAnonymised(anonymizedDicomFile);

            // And then deanonymize it using the original
            var deanonymizedDicomFile = innerEyeSegmentationClient.DeanonymizeDicomFile(
                anonymizedDicomFile,
                new[] { originalDicomFile },
                innerEyeSegmentationClient.TopLevelReplacements,
                Array.Empty<TagReplacement>(),
                innerEyeSegmentationClient.SegmentationAnonymisationProtocolId,
                innerEyeSegmentationClient.SegmentationAnonymisationProtocol);

            var deanonymizedDataset = deanonymizedDicomFile.Dataset;

            // Check the TopLevelReplacements have been copied.
            foreach (var dicomTag in innerEyeSegmentationClient.TopLevelReplacements)
            {
                var sourceValue = sourceDataset.GetSingleValue<string>(dicomTag);
                var deanonymizedValue = deanonymizedDataset.GetSingleValue<string>(dicomTag);

                Assert.IsFalse(string.IsNullOrEmpty(sourceValue));
                Assert.AreEqual(sourceValue, deanonymizedValue);
            }

            // Check the other tags are preserved
            foreach (var dicomTagAnonymization in innerEyeSegmentationClient.SegmentationAnonymisationProtocol)
            {
                var dicomTag = dicomTagAnonymization.DicomTagIndex.DicomTag;

                if (sourceDataset.Contains(dicomTag))
                {
                    var valueCount = sourceDataset.GetValueCount(dicomTag);
                    for (var i = 0; i < valueCount; i++)
                    {
                        var sourceValue = sourceDataset.GetValue<string>(dicomTag, i);
                        var deanonymizedValue = deanonymizedDataset.GetValue<string>(dicomTag, i);

                        Assert.IsFalse(string.IsNullOrEmpty(sourceValue));
                        Assert.AreEqual(sourceValue, deanonymizedValue);
                    }
                }
            }
        }
    }
}
