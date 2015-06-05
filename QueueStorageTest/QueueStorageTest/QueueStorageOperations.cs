using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace QueueStorageTest
{
    [TestClass]
    public class QueueStorageOperations
    {
        static CloudStorageAccount storageAccount;
        static CloudQueueClient queueClient;
        static CloudQueue queue;

        #region inits

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            storageAccount = CloudStorageAccount.Parse(AppSetting.StorageConnectionString);
            queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("testqueue");
            queue.CreateIfNotExists();
        }
        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (queue != null)
            {
                queue.Delete();
            }
        }
        [TestCleanup]
        public void TestCleanup()
        {

        }
        [TestInitialize]
        public void TestInitialize()
        {

        }
        #endregion

        #region tests

        [TestMethod]
        public void Insert_StringMessage_VerifyPeek()
        {
            string message = "This is sample message for the queue";
            queue.AddMessage(new CloudQueueMessage(message));
            string receivedMessage = queue.PeekMessage().AsString;

            Assert.AreEqual(message, receivedMessage, "String data queueing and then peeking did not work as expected.");
        }

        [TestMethod]
        public void Insert_StringMessage_Dequeue_Delete_Should_Delete()
        {
            string message = "This is sample message for the queue";
            queue.AddMessage(new CloudQueueMessage(message));
            CloudQueueMessage receivedMessage = queue.GetMessage();

            queue.DeleteMessage(receivedMessage);

            var receivedMessageStr = queue.PeekMessage()?.AsString;

            Assert.IsTrue(receivedMessageStr == null, receivedMessageStr, "String data queueing and then deleting did not work as expected.");
        }
        #endregion

        #region private methods
        #endregion
    }
}
