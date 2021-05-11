namespace Microsoft.InnerEye.Listener.Tests
{
    using System;
    using System.IO;
    using System.Linq;

    using Dicom;

    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DicomFileExtensionTests : BaseTestClass
    {
        [TestCategory("DicomFileExtensions")]
        [Description("Tests that we can create a copy of a Dicom file without the image pixel data.")]
        [TestMethod]
        public void TestRemovePixelData()
        {
            var segmentationAnonymisationProtocol = SegmentationAnonymisationProtocol();

            var imagePath = @"Images\1ValidSmall\1.dcm";
            var dicomFile = DicomFile.Open(imagePath);
            var metadataBytes = dicomFile.CreateNewDicomFileWithoutPixelData(segmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));

            var bytesOriginalFile = File.ReadAllBytes(imagePath);

            // Check the metadata is less than 1kb
            Assert.IsTrue(metadataBytes.Length < 1120);
            Assert.IsTrue(metadataBytes.Length > 0);
        }

        [TestCategory("DicomFileExtensions")]
        [Description("Tests that we can create a copy of a Dicom file without the image pixel data if some of the tags are invalid")]
        [TestMethod]
        public void TestRemovePixelDataInvalidTags()
        {
            var segmentationAnonymisationProtocol = SegmentationAnonymisationProtocol();

            var imagePath = @"Images\InvalidPN\1.dcm";
            var dicomFile = DicomFile.Open(imagePath);
            var metadataBytes = dicomFile.CreateNewDicomFileWithoutPixelData(segmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));

            var pixelData = Dicom.Imaging.DicomPixelData.Create(dicomFile.Dataset);
            var org_data = pixelData.GetFrame(0).Data;

            // Pixel data exists and is non-empty in the original file
            Assert.IsTrue(org_data.Length > 1);

            var ds = DicomFile.Open(new MemoryStream(metadataBytes)).Dataset;

            // Pixel data is missing in modified file
            Assert.ThrowsException<Dicom.Imaging.DicomImagingException>(() => { pixelData = Dicom.Imaging.DicomPixelData.Create(ds); });

            // Check the metadata is less than 1.2kb
            Assert.IsTrue(metadataBytes.Length < 1200);
            Assert.IsTrue(metadataBytes.Length > 0);
        }

        [TestCategory("DicomFileExtensions")]
        [Description("Tests we can enqueue a 4000 slice DICOM file onto the message queue")]
        [TestMethod]
        public void TestQueueDataLimit()
        {
            var segmentationAnonymisationProtocol = SegmentationAnonymisationProtocol();

            var dicomFile = DicomFile.Open(@"Images\1ValidSmall\1.dcm");
            var files = new byte[3600][];

            for (var i = 0; i < files.Length; i++)
            {
                files[i] = dicomFile.CreateNewDicomFileWithoutPixelData(segmentationAnonymisationProtocol.Select(x => x.DicomTagIndex.DicomTag));
            }

            using (var queue = GetTestMessageQueue())
            {
                TransactionalEnqueue(
                    queue,
                    new DownloadQueueItem(
                        segmentationId: Guid.Empty.ToString(),
                        modelId: Guid.Empty.ToString(),
                        resultsDirectory: CreateTemporaryDirectory().FullName,
                        referenceDicomFiles: files,
                        calledApplicationEntityTitle: "TEST1",
                        callingApplicationEntityTitle: "TEST2",
                        destinationApplicationEntity: new GatewayApplicationEntity("TestTitle", 140, "127.0.0.1"),
                        tagReplacementJsonString: "",
                        associationGuid: Guid.Empty,
                        associationDateTime: DateTime.UtcNow,
                        isDryRun: false));

                var result = TransactionalDequeue<DownloadQueueItem>(queue).ReferenceDicomFiles.ToArray();

                for (var i = 0; i < files.Length; i++)
                {
                    for (var ii = 0; ii < files[i].Length; ii++)
                    {
                        Assert.AreEqual(files[i][ii], result[i][ii]);
                    }
                }
            }
        }
    }
}
