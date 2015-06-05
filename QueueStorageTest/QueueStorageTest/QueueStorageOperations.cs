using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Threading;

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
            queue.Clear();
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

        [TestMethod]
        public void Insert_StringMessage_Dequeue_NoDelete_ShouldNot_Delete()
        {
            string message = "This is sample message for the queue";
            queue.AddMessage(new CloudQueueMessage(message));
            CloudQueueMessage receivedMessage = queue.GetMessage();

            Assert.AreEqual(message, receivedMessage.AsString, "String data queueing and then getting did not work as expected.");

            Assert.IsTrue(queue.PeekMessage() == null, "Queue message was still in the queue after reading.");
            
            Thread.Sleep(31000); // wait for 30 seconds so that message gets undone
            Assert.IsTrue(queue.GetMessage().AsString == message, "string data was not correctly undone when message was not deleted");
        }

        [TestMethod]
        public void Insert_Multiple_Messages_Should_Pass()
        {
            int messageCount = 200;
            for(int i = 1; i <= messageCount; i++)
            {
                queue.AddMessage(new CloudQueueMessage(i.ToString()));
            }
            
            queue.FetchAttributes();
            Assert.IsTrue(queue.ApproximateMessageCount == messageCount, $"Adding {messageCount} continuous message failed.");
        }

        [TestMethod]
        public void Update_Message_Compare_New_Value_ShouldMatch()
        {
            string message = "This is sample message for the queue";
            string newmessage = "This is new message";

            queue.AddMessage(new CloudQueueMessage(message));
            CloudQueueMessage receivedMessage = queue.GetMessage();
            Assert.AreEqual(message, receivedMessage.AsString, "received message is not same as queued one.");

            receivedMessage.SetMessageContent(newmessage);

            queue.UpdateMessage(receivedMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Content | MessageUpdateFields.Visibility);

            receivedMessage = queue.PeekMessage();
            Assert.AreEqual(newmessage, receivedMessage.AsString, "updating a message did not work");

        }
        #endregion

        #region private methods
        #endregion
    }
}
