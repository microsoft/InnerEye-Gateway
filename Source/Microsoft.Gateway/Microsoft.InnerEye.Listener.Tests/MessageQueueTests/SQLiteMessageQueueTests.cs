// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Tests.MessageQueueTests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Sqlite;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SQLiteTests : BaseTestClass
    {
        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests the SQLite transaciton throws an exception when committing or aborting before begin is called.")]
        [TestMethod]
        public void SqliteTransactionExceptionTests1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            const uint transactionLeaseMs = 3000;

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath, transactionLeaseMs: transactionLeaseMs))
            {
                messageQueue.Clear();

                using (var queueTransaction1 = messageQueue.CreateQueueTransaction())
                {
                    Assert.ThrowsException<InvalidOperationException>(() => queueTransaction1.Commit());
                    Assert.ThrowsException<InvalidOperationException>(() => queueTransaction1.Abort());
                }
            }
        }

        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests the SQLite transaciton throws an exception when committing or aborting when it is disposed.")]
        [TestMethod]
        public void SqliteTransactionExceptionTests2()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            const uint transactionLeaseMs = 3000;

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath, transactionLeaseMs: transactionLeaseMs))
            {
                var queueTransaction1 = messageQueue.CreateQueueTransaction();
                queueTransaction1.Dispose();

                Assert.ThrowsException<ObjectDisposedException>(() => queueTransaction1.Begin());
                Assert.ThrowsException<ObjectDisposedException>(() => queueTransaction1.Commit());
                Assert.ThrowsException<ObjectDisposedException>(() => queueTransaction1.Abort());
            }
        }

        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests adding an item on the queue and waits for past the time the lease should have expired. Checks the renew lease code is working.")]
        [TestMethod]
        public async Task SqliteRenewLeaseTests1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            const uint transactionLeaseMs = 1000;

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath, transactionLeaseMs: transactionLeaseMs))
            {
                messageQueue.Clear();

                using (var queueTransaction1 = messageQueue.CreateQueueTransaction())
                {
                    var expected = CreateRandomQueueItem();

                    queueTransaction1.Begin();

                    messageQueue.Enqueue(expected, queueTransaction1);

                    // Dequeue the item to start the renew lease expiry
                    var dequeued = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction1);

                    // Wait for twice the time - the lease should have expired if the renew code is not working
                    await Task.Delay(TimeSpan.FromMilliseconds(transactionLeaseMs * 2)).ConfigureAwait(false);

                    Assert.ThrowsException<MessageQueueReadException>(() => messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction1));
                }

                using (var queueTransaction = messageQueue.CreateQueueTransaction())
                {
                    queueTransaction.Begin();

                    Assert.ThrowsException<MessageQueueReadException>(() => messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction));
                }
            }
        }

        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests adding an item on the queue and waits for past the time the lease should have expired. Checks the item can be recovered on a different transaction.")]
        [TestMethod]
        public async Task SqliteExpiredLeaseTests1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            const uint transactionLeaseMs = 1000;

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath, transactionLeaseMs: transactionLeaseMs))
            {
                messageQueue.Clear();

                var expected = CreateRandomQueueItem();

                TransactionalEnqueue(messageQueue, expected);

                // Create a new transaction with the renew task larger than the time of the transaction lease (not a good idea in practice)
                using (var queueTransaction1 = new SqliteMessageQueueTransaction(messageQueue.DatabaseConnectionString, 60 * 1000))
                {
                    queueTransaction1.Begin();

                    // Dequeue the item to start the renew lease expiry
                    var actual1 = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction1);

                    AssertCompare(expected, actual1);

                    // Wait for twice the time - the lease should have now expired
                    await Task.Delay(TimeSpan.FromMilliseconds(transactionLeaseMs * 2)).ConfigureAwait(false);

                    // Dequeue on a different transaction
                    var actual2 = TransactionalDequeue<PushQueueItem>(messageQueue);

                    // Check the item was recovered.
                    AssertCompare(expected, actual2);

                    queueTransaction1.Abort();
                }
            }
        }

        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests adding an item onto the queue. Dequeue and clear the entire queue whilst the transaction is open. Test Abort and Commit does not throw exceptions.")]
        [TestMethod]
        public async Task SqliteClearDuringTransaction1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            const uint transactionLeaseMs = 1000;

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath, transactionLeaseMs: transactionLeaseMs))
            {
                messageQueue.Clear();

                var expected = CreateRandomQueueItem();

                // Also test Abort is called when disposing
                using (var transaction = messageQueue.CreateQueueTransaction())
                {
                    transaction.Begin();

                    messageQueue.Enqueue(expected, transaction);
                    var actual = messageQueue.DequeueNextMessage<PushQueueItem>(transaction);

                    messageQueue.Clear();

                    AssertCompare(expected, actual);

                    // Wait for the renew lease task to be called at least once.
                    await Task.Delay(TimeSpan.FromMilliseconds(transactionLeaseMs * 2)).ConfigureAwait(false);
                }

                using (var transaction = messageQueue.CreateQueueTransaction())
                {
                    transaction.Begin();

                    messageQueue.Enqueue(expected, transaction);
                    var actual = messageQueue.DequeueNextMessage<PushQueueItem>(transaction);

                    messageQueue.Clear();

                    AssertCompare(expected, actual);

                    // Wait for the renew lease task to be called at least once.
                    await Task.Delay(TimeSpan.FromMilliseconds(transactionLeaseMs * 2)).ConfigureAwait(false);

                    // Make sure no exception is thrown.
                    transaction.Commit();
                }
            }
        }

        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests that we can enqueue and dequeue in the same transaction without commiting .")]
        [TestMethod]
        public void SqliteTransactionTests1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                messageQueue.Clear();

                using (var queueTransaction1 = messageQueue.CreateQueueTransaction())
                {
                    var expected = CreateRandomQueueItem();

                    queueTransaction1.Begin();

                    messageQueue.Enqueue(expected, queueTransaction1);

                    var actual = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction1);

                    AssertCompare(expected, actual);

                    Assert.ThrowsException<MessageQueueReadException>(() => messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction1));

                    queueTransaction1.Commit();
                }
            }
        }

        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests that we can enqueue and attempt to dequeue before a commit in a different transaction.")]
        [TestMethod]
        public void SqliteTransactionTests2()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                messageQueue.Clear();

                using (var queueTransaction1 = messageQueue.CreateQueueTransaction())
                {
                    var expected = CreateRandomQueueItem();

                    queueTransaction1.Begin();

                    messageQueue.Enqueue(expected, queueTransaction1);

                    using (var queueTransaction2 = messageQueue.CreateQueueTransaction())
                    {
                        queueTransaction2.Begin();

                        Assert.ThrowsException<MessageQueueReadException>(() => messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction2));
                    }

                    queueTransaction1.Abort();
                }
            }
        }

        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests that abort is called when a transaction is disposed.")]
        [TestMethod]
        public void SqliteTransactionDisposeTest1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";
            var expected = CreateRandomQueueItem();

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                messageQueue.Clear();

                // Enqueue the expected item
                TransactionalEnqueue(messageQueue, expected);

                using (var queueTransaction = messageQueue.CreateQueueTransaction())
                {
                    queueTransaction.Begin();

                    // Dequeue but don't call abort specifically - let the dispose method do this
                    var actual = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction);

                    AssertCompare(expected, actual);
                }

                using (var queueTransaction = messageQueue.CreateQueueTransaction())
                {
                    queueTransaction.Begin();

                    var actual = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction);

                    AssertCompare(expected, actual);

                    queueTransaction.Commit();
                }
            }
        }

        [Timeout(60 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests the Sqlite Message Queue supports nested transactions.")]
        [TestMethod]
        public void SqliteNestedTransactionTest()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                messageQueue.Clear();

                using (var queueTransaction1 = messageQueue.CreateQueueTransaction())
                {
                    queueTransaction1.Begin();

                    using (var queueTransaction2 = messageQueue.CreateQueueTransaction())
                    {
                        queueTransaction2.Begin();
                        messageQueue.Enqueue(CreateRandomQueueItem(0), queueTransaction2);
                        queueTransaction2.Commit();
                    }

                    var queueItem = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction1);
                    Assert.IsNotNull(queueItem);

                    queueTransaction1.Commit();
                }
            }
        }

        [Timeout(120 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests 100 concurrent read/ write operations using a single Sqlite Message Queue.")]
        [TestMethod]
        public void SqliteTestConcurrentReadWrite1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";
            const int numberMessages = 100;

            var expectedResults = new PushQueueItem[numberMessages];
            var actualResults = new PushQueueItem[numberMessages];

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                messageQueue.Clear();

                for (var i = 0; i < numberMessages; i++)
                {
                    expectedResults[i] = CreateRandomQueueItem(i);
                }

                Parallel.For(0, numberMessages * 2, i =>
                {
                    if (i >= numberMessages)
                    {
                        var dequeueResult = TransactionalDequeue<PushQueueItem>(messageQueue, timeoutMs: 60 * 1000);

                        if (actualResults[dequeueResult.DequeueCount] != null)
                        {
                            throw new ArgumentException("This item has already been dequeued. Something is wrong.");
                        }

                        actualResults[dequeueResult.DequeueCount] = dequeueResult;
                    }
                    else
                    {
                        Enqueue(expectedResults[i], messageQueue);
                    }
                });
            }

            for (var i = 0; i < numberMessages; i++)
            {
                AssertCompare(expectedResults[i], actualResults[i]);
            }
        }

        [Timeout(120 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests 100 concurrent read/ write operations using multiple Sqlite Message Queues.")]
        [TestMethod]
        public void SqliteTestConcurrentReadWrite2()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";
            const int numberMessages = 100;

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                messageQueue.Clear();
            }

            var expectedResults = new PushQueueItem[numberMessages];
            var actualResults = new PushQueueItem[numberMessages];

            for (var i = 0; i < numberMessages; i++)
            {
                expectedResults[i] = CreateRandomQueueItem(i);
            }

            Parallel.For(0, numberMessages * 2, i =>
            {
                if (i >= numberMessages)
                {
                    var dequeueResult = Dequeue(messageQueuePath);

                    if (actualResults[dequeueResult.DequeueCount] != null)
                    {
                        throw new ArgumentException("This item has already been dequeued. Something is wrong.");
                    }

                    actualResults[dequeueResult.DequeueCount] = dequeueResult;
                }
                else
                {
                    Enqueue(expectedResults[i], messageQueuePath);
                }
            });

            for (var i = 0; i < numberMessages; i++)
            {
                AssertCompare(expectedResults[i], actualResults[i]);
            }
        }

        [Timeout(120 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests 40 concurrent read/ write across 4 threads reading/ writing 10 items each.")]
        [TestMethod]
        public void SqliteTestConcurrentReadWrite3()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            const int numberThreads = 4;
            const int numberMessagesPerThread = 10;
            const int numberMessages = numberThreads * numberMessagesPerThread;

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                messageQueue.Clear();
            }

            var expectedResults = new PushQueueItem[numberMessages];
            var actualResults = new PushQueueItem[numberMessages];

            for (var i = 0; i < numberMessages; i++)
            {
                expectedResults[i] = CreateRandomQueueItem(i);
            }

            Parallel.For(0, numberThreads, i =>
            {
                for (var ii = 0; ii < numberMessagesPerThread * 2; ii++)
                {
                    // Switch between reading and writing
                    if (ii % 2 == 0)
                    {
                        Enqueue(expectedResults[(i * numberMessagesPerThread) + (ii / 2)], messageQueuePath);
                    }
                    else
                    {
                        var dequeueResult = Dequeue(messageQueuePath);

                        if (actualResults[dequeueResult.DequeueCount] != null)
                        {
                            throw new ArgumentException("This item has already been dequeued. Something is wrong.");
                        }

                        actualResults[dequeueResult.DequeueCount] = dequeueResult;
                    }
                }
            });

            for (var i = 0; i < numberMessages; i++)
            {
                AssertCompare(expectedResults[i], actualResults[i]);
            }
        }

        [Timeout(240 * 1000)]
        [TestCategory("SqliteMessageQueue")]
        [Description("Tests 10 parallel read/ write operations in the same transaction.")]
        [TestMethod]
        public void SqliteTestConcurrentReadWrite4()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";
            const int numberMessages = 10;

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                messageQueue.Clear();
            }

            var expectedResults = new PushQueueItem[numberMessages];
            var actualResults = new PushQueueItem[numberMessages];

            for (var i = 0; i < numberMessages; i++)
            {
                expectedResults[i] = CreateRandomQueueItem(i);
            }

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                using (var messageQueueTransaction = messageQueue.CreateQueueTransaction())
                {
                    messageQueueTransaction.Begin();

                    Parallel.For(0, numberMessages * 2, i =>
                    {
                        if (i % 2 == 0)
                        {
                            messageQueue.Enqueue(expectedResults[i / 2], messageQueueTransaction);
                        }
                        else
                        {
                            var dequeueResult = TryDequeue<PushQueueItem>(messageQueue, messageQueueTransaction, timeoutMs: 120 * 1000);

                            if (actualResults[dequeueResult.DequeueCount] != null)
                            {
                                throw new ArgumentException("This item has already been dequeued. Something is wrong.");
                            }

                            actualResults[dequeueResult.DequeueCount] = dequeueResult;
                        }
                    });

                    messageQueueTransaction.Commit();
                }
            }

            for (var i = 0; i < numberMessages; i++)
            {
                AssertCompare(expectedResults[i], actualResults[i]);
            }
        }

        [Timeout(30 * 1000)]
        [Description("Attempts to dequeue from a SQLite queue that has not been created.")]
        [TestCategory("SqliteMessageQueue")]
        [TestMethod]
        public void SqliteTestDequeueOnly1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            using (var queueTransaction = messageQueue.CreateQueueTransaction())
            {
                queueTransaction.Begin();

                Assert.ThrowsException<MessageQueueReadException>(() => messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction));

                queueTransaction.Abort();
            }
        }

        [Timeout(30 * 1000)]
        [Description("Tests we can enqueue and dequeue in the correct order from the Sqlite Message Queue")]
        [TestCategory("SqliteMessageQueue")]
        [TestMethod]
        public void SqliteTestEnqueueDequeue1()
        {
            var messageQueuePath = $@".\Private$\{Guid.NewGuid()}";

            var expectedItem1 = CreateRandomQueueItem(0);
            var expectedItem2 = CreateRandomQueueItem(1);

            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            using (var queueTransaction = messageQueue.CreateQueueTransaction())
            {
                // Clear the queue
                messageQueue.Clear();

                queueTransaction.Begin();
                messageQueue.Enqueue(expectedItem1, queueTransaction);
                messageQueue.Enqueue(expectedItem2, queueTransaction);
                queueTransaction.Commit();
            }

            // Read from queue and then abort
            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            using (var queueTransaction = messageQueue.CreateQueueTransaction())
            {
                queueTransaction.Begin();

                var actualItem = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction);
                AssertCompare(expectedItem1, actualItem);

                queueTransaction.Abort();
            }

            // Read from queue and the commit
            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            using (var queueTransaction = messageQueue.CreateQueueTransaction())
            {
                queueTransaction.Begin();

                var actualItem = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction);
                AssertCompare(expectedItem1, actualItem);

                queueTransaction.Commit();
            }

            // Read from queue and check we can dequeue the second item.
            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            using (var queueTransaction = messageQueue.CreateQueueTransaction())
            {
                queueTransaction.Begin();

                var actualItem = messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction);
                AssertCompare(expectedItem2, actualItem);

                queueTransaction.Commit();
            }

            // Read from queue and check we can dequeue the second item.
            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            using (var queueTransaction = messageQueue.CreateQueueTransaction())
            {
                queueTransaction.Begin();

                Assert.ThrowsException<MessageQueueReadException>(() => messageQueue.DequeueNextMessage<PushQueueItem>(queueTransaction));

                queueTransaction.Abort();
            }
        }

        private static PushQueueItem CreateRandomQueueItem(int index = 0)
        {
            return new PushQueueItem(
                new GatewayApplicationEntity(Guid.NewGuid().ToString(), index, Guid.NewGuid().ToString()),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid().ToString())
            {
                DequeueCount = index
            };
        }

        private static void AssertCompare(PushQueueItem expected, PushQueueItem actual)
        {
            Assert.AreEqual(expected.AssociationGuid, actual.AssociationGuid);
            Assert.AreEqual(expected.CalledApplicationEntityTitle, actual.CalledApplicationEntityTitle);
            Assert.AreEqual(expected.CallingApplicationEntityTitle, actual.CallingApplicationEntityTitle);
            Assert.AreEqual(expected.AssociationDateTime, actual.AssociationDateTime);
            Assert.AreEqual(expected.DequeueCount, actual.DequeueCount);
            Assert.AreEqual(expected.DestinationApplicationEntity.IpAddress, actual.DestinationApplicationEntity.IpAddress);
            Assert.AreEqual(expected.DestinationApplicationEntity.Port, actual.DestinationApplicationEntity.Port);
            Assert.AreEqual(expected.DestinationApplicationEntity.Title, actual.DestinationApplicationEntity.Title);

            for (var i = 0; i < expected.FilePaths.Count(); i++)
            {
                Assert.AreEqual(expected.FilePaths.ElementAt(i), actual.FilePaths.ElementAt(i));
            }
        }

        private static void Enqueue(PushQueueItem queueItem, string messageQueuePath)
        {
            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                Enqueue(queueItem, messageQueue);
            }
        }

        private static void Enqueue(PushQueueItem queueItem, SqliteMessageQueue messageQueue)
        {
            using (var queueTransaction = messageQueue.CreateQueueTransaction())
            {
                queueTransaction.Begin();
                messageQueue.Enqueue(queueItem, queueTransaction);
                queueTransaction.Commit();
            }
        }

        private static PushQueueItem Dequeue(string messageQueuePath)
        {
            using (var messageQueue = new SqliteMessageQueue(messageQueuePath))
            {
                return TransactionalDequeue<PushQueueItem>(messageQueue, timeoutMs: 60 * 1000);
            }
        }
    }
}