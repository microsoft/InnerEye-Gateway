namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.DataProvider.Implementations;
    using Microsoft.InnerEye.Listener.DataProvider.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PushServiceTests : BaseTestClass
    {
        [TestCategory("PushService")]
        [Description("Tests a push queue item can be queued and dequeued..")]
        [Timeout(120 * 1000)]
        [TestMethod]
        public void PushQueueItemTest()
        {
            using (var InnerEyeQueue = GetTestMessageQueue())
            {
                var expected =
                    new PushQueueItem(
                        new GatewayApplicationEntity("Test1", 160, "127.0.0.1"),
                        "Test2",
                        "Test2",
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        "Test3",
                        "Test4",
                        "Test5");

                TransactionalEnqueue(InnerEyeQueue, expected);

                var actual = TransactionalDequeue<PushQueueItem>(InnerEyeQueue);

                Assert.AreEqual(expected.AssociationGuid, actual.AssociationGuid);
                Assert.AreEqual(expected.CalledApplicationEntityTitle, actual.CalledApplicationEntityTitle);
                Assert.AreEqual(expected.CallingApplicationEntityTitle, actual.CallingApplicationEntityTitle);
                Assert.AreEqual(expected.AssociationDateTime, actual.AssociationDateTime);
                Assert.AreEqual(expected.DestinationApplicationEntity.IpAddress, actual.DestinationApplicationEntity.IpAddress);
                Assert.AreEqual(expected.DestinationApplicationEntity.Port, actual.DestinationApplicationEntity.Port);
                Assert.AreEqual(expected.DestinationApplicationEntity.Title, actual.DestinationApplicationEntity.Title);
                Assert.AreEqual(expected.FilePaths.ElementAt(0), actual.FilePaths.ElementAt(0));
                Assert.AreEqual(expected.FilePaths.ElementAt(1), actual.FilePaths.ElementAt(1));
                Assert.AreEqual(expected.FilePaths.ElementAt(2), actual.FilePaths.ElementAt(2));

            }
        }

        [TestCategory("PushService")]
        [Description("Tests the push service can push results with images and structure sets.")]
        [Timeout(120 * 1000)]
        [TestMethod]
        public async Task PushServiceTest()
        {
            var tempFolder = CreateTemporaryDirectory();

            // Copy all files in the P4_Prostate directory to the temporary directory
            Directory.EnumerateFiles(@"Images\1ValidSmall\")
                .Select(x => new FileInfo(x))
                .ToList()
                .ForEach(x => x.CopyTo(Path.Combine(tempFolder.FullName, x.Name)));

            var applicationEntity = new GatewayApplicationEntity("RListenerTest", 108, "127.0.0.1");
            var resultDirectory = CreateTemporaryDirectory();

            // Create a Data receiver to receive the RT struct result
            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultDirectory.FullName)))
            {
                var eventCount = 0;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    Interlocked.Increment(ref eventCount);
                };

                var started = dicomDataReceiver.StartServer(applicationEntity.Port, BuildAcceptedSopClassesAndTransferSyntaxes, TimeSpan.FromSeconds(1));

                Assert.IsTrue(started);
                Assert.IsTrue(dicomDataReceiver.IsListening);

                var dataSender = new DicomDataSender();
                var echoResult = await dataSender.DicomEchoAsync("RListener", applicationEntity.Title, applicationEntity.Port, applicationEntity.IpAddress);

                // Check echo
                Assert.IsTrue(echoResult == DicomOperationResult.Success);

                var testAETConfigModel = GetTestAETConfigModel();

                using (var deleteService = CreateDeleteService())
                using (var pushService = CreatePushService())
                using (var pushQueue = pushService.PushQueue)
                {
                    deleteService.Start();
                    pushService.Start();

                    TransactionalEnqueue(
                        pushQueue,
                        new PushQueueItem(
                            destinationApplicationEntity: applicationEntity,
                            calledApplicationEntityTitle: testAETConfigModel.CalledAET,
                            callingApplicationEntityTitle: applicationEntity.Title,
                            associationGuid: Guid.NewGuid(),
                            associationDateTime: DateTime.UtcNow,
                            filePaths: tempFolder.GetFiles().Select(x => x.FullName).ToArray()));

                    // Wait for all events to finish on the data received
                    SpinWait.SpinUntil(() => eventCount >= 3, TimeSpan.FromMinutes(3));

                    SpinWait.SpinUntil(() => new DirectoryInfo(tempFolder.FullName).Exists == false, TimeSpan.FromSeconds(30));

                    Assert.IsFalse(new DirectoryInfo(tempFolder.FullName).Exists);

                    Assert.AreEqual(20, resultDirectory.GetDirectories()[0].GetFiles().Length);
                }
            }
        }

        [TestCategory("PushService")]
        [Description("Checks the push service recovers from a bad destination.")]
        [Timeout(120 * 1000)]
        [TestMethod]
        public async Task PushServiceBadAetTest()
        {
            var testAETConfigModel = GetTestAETConfigModel();
            var destination = testAETConfigModel.AETConfig.Destination;

            var tempFolder = CreateTemporaryDirectory();

            // Grab a structure set file
            var file = new FileInfo(@"Images\LargeSeriesWithContour\rtstruct.dcm");
            file.CopyTo(Path.Combine(tempFolder.FullName, file.Name));

            var resultDirectory = CreateTemporaryDirectory();

            // Create a Data receiver to receive the RT struct result
            using (var dicomDataReceiver = new ListenerDataReceiver(new ListenerDicomSaver(resultDirectory.FullName)))
            {
                var eventCount = 0;
                var folderPath = string.Empty;

                dicomDataReceiver.DataReceived += (sender, e) =>
                {
                    folderPath = e.FolderPath;
                    Interlocked.Increment(ref eventCount);
                };

                dicomDataReceiver.StartServer(
                    destination.Port,
                    BuildAcceptedSopClassesAndTransferSyntaxes,
                    TimeSpan.FromSeconds(1));

                var dataSender = new DicomDataSender();
                var echoResult = await dataSender.DicomEchoAsync(
                    "RListener",
                    destination.Title,
                    destination.Port,
                    destination.Ip);

                Assert.IsTrue(dicomDataReceiver.IsListening);

                // Check echo
                Assert.IsTrue(echoResult == DicomOperationResult.Success);

                using (var deleteService = CreateDeleteService())
                using (var pushService = CreatePushService())
                using (var pushQueue = pushService.PushQueue)
                {
                    deleteService.Start();
                    pushService.Start();

                    TransactionalEnqueue(
                        pushQueue,
                        new PushQueueItem(
                            destinationApplicationEntity: new GatewayApplicationEntity("", -1, "ababa"),
                            calledApplicationEntityTitle: testAETConfigModel.CalledAET,
                            callingApplicationEntityTitle: testAETConfigModel.CallingAET,
                            associationGuid: Guid.NewGuid(),
                            associationDateTime: DateTime.UtcNow,
                            filePaths: tempFolder.GetFiles().Select(x => x.FullName).ToArray()));

                    // Wait for all events to finish on the data received
                    SpinWait.SpinUntil(() => eventCount >= 3, TimeSpan.FromMinutes(3));

                    SpinWait.SpinUntil(() => new DirectoryInfo(tempFolder.FullName).Exists == false, TimeSpan.FromSeconds(30));

                    Assert.IsFalse(new DirectoryInfo(tempFolder.FullName).Exists);
                }
            }
        }
    }
}