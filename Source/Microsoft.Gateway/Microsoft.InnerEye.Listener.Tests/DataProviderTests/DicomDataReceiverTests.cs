namespace Microsoft.InnerEye.Listener.Tests.DataProviderTests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Dicom.Network;

    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Models;
    using Microsoft.InnerEye.Listener.Tests.Common.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DicomDataReceiverTests : BaseTestClass
    {
        [Timeout(60 * 1000)]
        [TestCategory("DicomDataReceiver")]
        [Description("Starts two data receivers listening on the same port.")]
        [TestMethod]
        public void DicomDataReceiverSamePort()
        {
            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 120, "127.0.0.1");
            var resultsDirectory = CreateTemporaryDirectory();

            using (var dicomDataReceiver1 = new ListenerDataReceiver(new ListenerDicomSaver(resultsDirectory.FullName)))
            {
                StartDicomDataReceiver(dicomDataReceiver1, applicationEntity.Port);

                using (var dicomDataReceiver2 = new ListenerDataReceiver(new ListenerDicomSaver(resultsDirectory.FullName)))
                {
                    Assert.ThrowsException<DicomNetworkException>(() => StartDicomDataReceiver(dicomDataReceiver2, applicationEntity.Port));

                    // Check you can start again and on a different port with the same AE title and IP address.
                    StartDicomDataReceiver(dicomDataReceiver2, applicationEntity.Port + 1);
                }
            }
        }

        [Timeout(600 * 1000)]
        [TestCategory("DicomDataReceiverDCMTK")]
        [Description("Starts the listener and pushes a file over Dicom to check we can receive data.")]
        [TestMethod]
        public async Task DicomDataReceiverServerStarts()
        {
            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 191, "127.0.0.1");
            var resultsDirectory = CreateTemporaryDirectory();

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultsDirectory.FullName)))
            {
                StartDicomDataReceiver(dicomDataReceiver, applicationEntity.Port);

                var eventCount = 0;
                var folderPath = string.Empty;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    folderPath = e.FolderPath;
                    Interlocked.Increment(ref eventCount);
                };

                Assert.ThrowsException<DicomNetworkException>(() => StartDicomDataReceiver(dicomDataReceiver, applicationEntity.Port));

                var dataSender = new DicomDataSender();
                var echoResult = await dataSender.DicomEchoAsync(
                    "RListener",
                    applicationEntity.Title,
                    applicationEntity.Port,
                    applicationEntity.IpAddress);

                // Check echo
                Assert.IsTrue(echoResult == DicomOperationResult.Success);

                DcmtkHelpers.SendFileUsingDCMTK(
                    @"Images\1ValidSmall\1.dcm",
                    applicationEntity.Port,
                    ScuProfile.LEExplicitCT,
                    TestContext);

                // Wait for all events to finish on the data received
                SpinWait.SpinUntil(() => eventCount >= 3, TimeSpan.FromSeconds(10));

                // Check the file exists
                Assert.IsTrue(File.Exists(Path.Combine(folderPath, @"1.2.840.113619.2.81.290.1.36662.3.1.20151027.220159.dcm")));

                dicomDataReceiver.StopServer();
            }
        }

        [Ignore("This test works locally but is not robust on the build system. Mark as ignore for now.")]
        [Timeout(60 * 1000)]
        [TestCategory("DicomDataReceiver")]
        [Description("Starts the listener and sends 10 associations to the receiver. Checks all are received correctly.")]
        [TestMethod]
        public void DicomDataReceiverMutlipleAssociations()
        {
            const int numberOfAssociations = 10;
            const int port = 122;

            var applicationEntity = new GatewayApplicationEntity("RListenerTest", port, "127.0.0.1");
            var resultsDirectory = CreateTemporaryDirectory();

            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultsDirectory.FullName)))
            {
                StartDicomDataReceiver(dicomDataReceiver, applicationEntity.Port);

                var associationsReceivedCount = 0;
                var receivedAssociations = new string[numberOfAssociations];

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    if (e.ProgressCode == DicomReceiveProgressCode.AssociationEstablished)
                    {
                        receivedAssociations[int.Parse(e.DicomAssociation.CallingAE)] = e.DicomAssociation.CallingAE;
                        Interlocked.Increment(ref associationsReceivedCount);
                    }
                };

                Parallel.For(0, numberOfAssociations, i =>
                {
                    var dcmtkResult = DcmtkHelpers.SendFileUsingDCMTK(
                        @"Images\1ValidSmall\1.dcm",
                        port,
                        ScuProfile.LEExplicitCT,
                        TestContext,
                        waitForExit: false,
                        applicationEntityTitle: i.ToString());
                });

                SpinWait.SpinUntil(() => associationsReceivedCount == numberOfAssociations, TimeSpan.FromMinutes(1));

                for (var i = 0; i < numberOfAssociations; i++)
                {
                    Assert.IsTrue(receivedAssociations[i] == i.ToString());
                }
            }
        }
    }
}