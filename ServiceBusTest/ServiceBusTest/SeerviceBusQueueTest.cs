using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        [TestMethod]
        public void Send_Header_Verify_Received()
        {
            var message = new Student() { Id = Guid.NewGuid().ToString(), Name = "John Doe", Age = 40 };
            var brokeredMessage = new BrokeredMessage(message);
            brokeredMessage.Properties.Add("MessageName", "Name of the message");

            brokeredMessage.MessageId = Guid.NewGuid().ToString();
            brokeredMessage.To = Guid.NewGuid().ToString();
            queueClient.Send(brokeredMessage);
            var receivedMessage = queueClient.Receive();
            var receivedStudent = receivedMessage.GetBody<Student>();
            Assert.IsTrue(receivedStudent.Equals(message), "Message body was not received correctly when custom header is sent.");

            object receivedMessageName;
            receivedMessage.Properties.TryGetValue("MessageName", out receivedMessageName);
            Assert.IsTrue(receivedMessageName.ToString() == "Name of the message", "Custom properties in brokered message was not received correctly");

            Assert.IsTrue(receivedMessage.MessageId == brokeredMessage.MessageId, "Message Id is not received correctly");

            Assert.IsTrue(receivedMessage.To == brokeredMessage.To, "'To' is not received correctly");
            
        }

        [TestMethod]
        public void ReceiveMessage_Abandon_ShouldNotDelete()
        {
            string message = "A sample string queue message";
            queueClient.Send(new BrokeredMessage(message));

            var receivedMessage = queueClient.Receive();
            queueClient.Abandon(receivedMessage.LockToken);

            receivedMessage = queueClient.Receive();

            Assert.IsTrue(receivedMessage.GetBody<string>() == message, "Message abandoning did not work as expected");

        }

        [TestMethod]
        public void ReceiveMessage_Second_Read_Should_Return_Null()
        {
            string message = "A sample string queue message";
            queueClient.Send(new BrokeredMessage(message));

            var receivedMessage = queueClient.Receive();
            receivedMessage = queueClient.Receive(TimeSpan.FromSeconds(2));
            
            Assert.IsTrue(receivedMessage == null, "Receieved message on 2nd read should be null when only one message was in the queue");
        }

        [TestMethod]
        public void Send_Multiple_Message_Verify_Subscriber_ReceivesAll()
        {
            int messageCount = 10;
            List<string> receivedMessages = new List<string>();
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            queueClient.OnMessage((message) => {
                receivedMessages.Add(message.GetBody<string>());
                message.Complete();
                if(receivedMessages.Count >= messageCount)
                {
                    waitEvent.Set();
                }
            });
            List<string> sentMessages = new List<string>();
            for (int i = 1; i <= messageCount; i++)
            {
                queueClient.Send(new BrokeredMessage(i.ToString()));
                sentMessages.Add(i.ToString());
            }
            waitEvent.WaitOne(10000);
            Assert.IsTrue(Enumerable.SequenceEqual(sentMessages, receivedMessages), "Sent and received messages did not match when received using subscription");
        }
    }
}
