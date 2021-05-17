// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Tests.MessageQueueTests
{
    using System;
    using System.Linq;

    using Microsoft.InnerEye.Gateway.Models;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QueueItemQueueTests : BaseTestClass
    {
        [TestCategory("QueueItem")]
        [Timeout(60 * 1000)]
        [Description("Tests that the delete queue item can be added to the message queue and read correctly.")]
        [TestMethod]
        public void TestDeleteQueueItem()
        {
            // Get the receive queue
            using (var queue = GetUniqueMessageQueue())
            {
                var expected = new DeleteQueueItem(
                    new AssociationQueueItemBase(
                        "HelloWorld1",
                        "HelloWorld2",
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        5),
                    @"c:\sdgfsd",
                    @"arandompath",
                    @"d:\temp\dicomfile.dcm")
                {
                    DequeueCount = 5
                };

                TransactionalEnqueue(queue, expected);

                var actual = TransactionalDequeue<DeleteQueueItem>(queue);

                Assert.IsNotNull(actual);

                Assert.AreEqual(expected.AssociationGuid, actual.AssociationGuid);
                Assert.AreEqual(expected.AssociationDateTime, actual.AssociationDateTime);
                Assert.AreEqual(expected.CalledApplicationEntityTitle, actual.CalledApplicationEntityTitle);
                Assert.AreEqual(expected.CallingApplicationEntityTitle, actual.CallingApplicationEntityTitle);
                Assert.AreEqual(expected.DequeueCount, actual.DequeueCount);
                Assert.AreEqual(expected.Paths.Count(), actual.Paths.Count());

                for (var i = 0; i < expected.Paths.Count(); i++)
                {
                    Assert.AreEqual(expected.Paths.ElementAt(i), actual.Paths.ElementAt(i));
                }
            }
        }

        [TestCategory("QueueItem")]
        [Timeout(60 * 1000)]
        [Description("Tests that the push queue item can be added to the message queue and read correctly.")]
        [TestMethod]
        public void TestPushQueueItem()
        {
            // Get the receive queue
            using (var queue = GetUniqueMessageQueue())
            {
                var queueItem = new PushQueueItem(
                    destinationApplicationEntity: new GatewayApplicationEntity("Test3", 105, "Test4"),
                    calledApplicationEntityTitle: "TestAet",
                    callingApplicationEntityTitle: "Test6",
                    associationGuid: Guid.NewGuid(),
                    associationDateTime: DateTime.UtcNow,
                    filePaths: new[] {
                        @"c:\sdgfsd",
                        @"arandompath",
                        @"d:\temp\dicomfile.dcm"
                    })
                {
                    DequeueCount = 3
                };

                TransactionalEnqueue(queue, queueItem);

                var item = TransactionalDequeue<PushQueueItem>(queue);

                Assert.IsNotNull(item);

                AssertAllProperties(queueItem, item);
            }
        }

        [TestCategory("QueueItem")]
        [Timeout(60 * 1000)]
        [Description("Tests that the download queue item can be added to the message queue and read correctly.")]
        [TestMethod]
        public void TestDownloadQueueItem()
        {
            // Get the receive queue
            using (var queue = GetUniqueMessageQueue())
            {
                var queueItem = new DownloadQueueItem(
                    segmentationId: Guid.NewGuid().ToString(),
                    modelId: Guid.NewGuid().ToString(),
                    resultsDirectory: CreateTemporaryDirectory().FullName,
                    referenceDicomFiles: new[] { new byte[] { 5, 6, 8, 10 }, new byte[] { 2, 9, 11, 22 } },
                    calledApplicationEntityTitle: "Test2",
                    callingApplicationEntityTitle: "Test30",
                    destinationApplicationEntity: new GatewayApplicationEntity("Test3", 105, "Test4"),
                    tagReplacementJsonString: "HELLO WORLD 1 / 2 3; 5",
                    associationGuid: Guid.NewGuid(),
                    associationDateTime: DateTime.UtcNow,
                    isDryRun: false)
                {
                    DequeueCount = 4
                };

                TransactionalEnqueue(queue, queueItem);

                var item = TransactionalDequeue<DownloadQueueItem>(queue);

                Assert.IsNotNull(item);

                AssertAllProperties(queueItem, item);
            }
        }

        [TestCategory("QueueItem")]
        [Timeout(60 * 1000)]
        [Description("Tests that the upload queue item can be added to the message queue and read correctly.")]
        [TestMethod]
        public void TestUploadQueueItem()
        {
            // Get the receive queue
            using (var queue = GetUniqueMessageQueue())
            {
                var queueItem = new UploadQueueItem(
                    calledApplicationEntityTitle: "Test1",
                    callingApplicationEntityTitle: "Test2",
                    associationFolderPath: "Test4",
                    rootDicomFolderPath: "Test6",
                    associationGuid: Guid.NewGuid(),
                    associationDateTime: DateTime.UtcNow)
                {
                    DequeueCount = 9
                };

                TransactionalEnqueue(queue, queueItem);

                var item = TransactionalDequeue<UploadQueueItem>(queue);

                Assert.IsNotNull(item);

                AssertAllProperties(queueItem, item);
            }
        }

        private void AssertAllProperties<T>(T expectedValue, T actualValue)
        {
            const string SystemNamespace = "System";

            var expectedType = expectedValue.GetType();

            if (expectedType.Namespace == SystemNamespace && !expectedType.IsArray)
            {
                Assert.AreEqual(expectedValue, actualValue);
                return;
            }

            foreach (var property in expectedType.GetProperties().Where(x => x.GetIndexParameters().Length == 0))
            {
                var expected = property.GetValue(expectedValue);
                var actual = property.GetValue(actualValue);

                var type = expected.GetType();

                if (type.IsArray)
                {
                    var expectedItem = expected as Array;
                    var actualItem = actual as Array;

                    for (var i = 0; i < expectedItem.Length; i++)
                    {
                        AssertAllProperties(expectedItem.GetValue(i), actualItem.GetValue(i));
                    }
                }
                else if (type.Namespace == SystemNamespace)
                {
                    Assert.AreEqual(expected, actual);
                }
                else
                {
                    AssertAllProperties(expected, actual);
                }
            }
        }
    }
}