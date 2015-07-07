using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace ServiceBusTest
{
    [TestClass]
    public class SeerviceBusQueueTest
    {
        private static QueueDescription testQueue;
        private static NamespaceManager namespaceManager;
        private QueueClient queueClient;

        #region initializers

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            namespaceManager = NamespaceManager.CreateFromConnectionString(AppSetting.ServiceBusConnectionString);
        }
        [ClassCleanup]
        public static void ClassCleanup()
        {
           
        }
        [TestInitialize]
        public void TestInitialize()
        {
            string queueName = Guid.NewGuid().ToString();
            testQueue = namespaceManager.CreateQueue(queueName);
            queueClient = QueueClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, queueName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            namespaceManager.DeleteQueue(testQueue.Path);
        }

        #endregion

        [TestMethod]
        public void Send_String_Message_Verify_Content()
        {
            string message = "A sample string queue message";
            queueClient.Send(new BrokeredMessage(message));

            var receivedMessage = queueClient.Receive();
            Assert.IsTrue(receivedMessage.GetBody<string>() == message, "String Message was not correctly inserted in the queue");

        }

        [TestMethod]
        public void Send_CustomObject_Message_Verify_Content()
        {
            var message = new Student() { Id= Guid.NewGuid().ToString(), Name = "John Doe", Age= 40 };
            queueClient.Send(new BrokeredMessage(message));

            var receivedMessage = queueClient.Receive();
            Assert.IsTrue(receivedMessage.GetBody<Student>().Equals( message), "Custom object Message was not correctly inserted in the queue");
        }

        [TestMethod]
        public void Send_OneStringAndOneCustomObject_Message_Verify_Content()
        {
            var message1 = "This is sample string message";
            var message2 = new Student() { Id = Guid.NewGuid().ToString(), Name = "John Doe", Age = 40 };
            queueClient.Send(new BrokeredMessage(message1));
            queueClient.Send(new BrokeredMessage(message2));

            var receivedMessage1 = queueClient.Receive();
            var receivedMessage2 = queueClient.Receive();
            Assert.IsTrue(receivedMessage1.GetBody<string>().Equals(message1) && receivedMessage2.GetBody<Student>().Equals(message2), "Custom object Message and String message one after another was not correctly inserted in the queue");
        }
    }
}
