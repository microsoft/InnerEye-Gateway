namespace Microsoft.InnerEye.Listener.Tests.DataProviderTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Dicom;

    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DicomDataSenderTests : BaseTestClass
    {
        [Timeout(60 * 1000)]
        [TestCategory("DicomDataSender")]
        [Description("Sends a Dicom echo from the data sender and checks we get a valid response.")]
        [TestMethod]
        public async Task DicomEchoTest()
        {
            var dataSender = new DicomDataSender();

            var applicationEntity = new GatewayApplicationEntity(
                title: "RListenerTest",
                port: new Random().Next(130, ApplicationEntityValidationHelpers.MaximumPortNumber),
                ipAddress: "127.0.0.1");

            var resultsDirectory = CreateTemporaryDirectory();

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultsDirectory.FullName)))
            {
                StartDicomDataReceiver(dicomDataReceiver, applicationEntity.Port);

                var result1 = await dataSender.DicomEchoAsync(
                    "Hello",
                    applicationEntity.Title,
                    applicationEntity.Port,
                    applicationEntity.IpAddress);

                Assert.AreEqual(DicomOperationResult.Success, result1);

                dicomDataReceiver.StopServer();
            }

            var result2 = await dataSender.DicomEchoAsync(
                "Hello",
                applicationEntity.Title,
                applicationEntity.Port,
                applicationEntity.IpAddress);

            Assert.AreEqual(DicomOperationResult.NoResponse, result2);

            // Try ping with IPv6 Address
            var result3 = await dataSender.DicomEchoAsync("Hello", "RListenerTest", 105, "2a00:1450:4009:800::200e");

            Assert.AreEqual(DicomOperationResult.NoResponse, result3);
        }

        [Timeout(60 * 1000)]
        [TestCategory("DicomDataSender")]
        [Description("Sends a files over Dicom from the data sender.")]
        [TestMethod]
        public async Task DicomSendFilesTest()
        {
            var dataSender = new DicomDataSender();
            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 131, "127.0.0.1");

            var dicomFiles = new DirectoryInfo(@"Images\1ValidSmall\").GetFiles().Select(x => DicomFile.Open(x.FullName)).ToArray();
            var rtFile = await DicomFile.OpenAsync(@"Images\LargeSeriesWithContour\rtstruct.dcm");

            var resultsDirectory = CreateTemporaryDirectory();

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultsDirectory.FullName)))
            {
                StartDicomDataReceiver(dicomDataReceiver, applicationEntity.Port);

                var results = await dataSender.SendFilesAsync(
                    "Hello",
                    applicationEntity.Title,
                    applicationEntity.Port,
                    applicationEntity.IpAddress,
                    dicomFiles);

                foreach (var result in results)
                {
                    // Check we can send non-RT files
                    Assert.AreEqual(DicomOperationResult.Success, result.Item2);
                }

                var rtResult = await dataSender.SendFilesAsync(
                    "Hello",
                    applicationEntity.Title,
                    applicationEntity.Port,
                    applicationEntity.IpAddress,
                    rtFile);

                Assert.AreEqual(1, rtResult.Count());

                foreach (var result in rtResult)
                {
                    // Check we can send RT files
                    Assert.AreEqual(DicomOperationResult.Success, result.Item2);
                }

                dicomDataReceiver.StopServer();
            }
        }
    }
}