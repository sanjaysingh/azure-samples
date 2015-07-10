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
    public class ServiceBusQueueTest
    {
        #region private fields

        private QueueDescription testQueue;
        private static NamespaceManager namespaceManager;
        private QueueClient queueClient;

        #endregion

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

        #region tests

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
            var message = new Student() { Id = Guid.NewGuid().ToString(), Name = "John Doe", Age = 40 };
            queueClient.Send(new BrokeredMessage(message));

            var receivedMessage = queueClient.Receive();
            Assert.IsTrue(receivedMessage.GetBody<Student>().Equals(message), "Custom object Message was not correctly inserted in the queue");
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
        public void Send_Multiple_Messages_Verify_Subscriber_ReceivesAll()
        {
            int messageCount = 10;
            List<string> receivedMessages = new List<string>();
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            queueClient.OnMessage((message) =>
            {
                receivedMessages.Add(message.GetBody<string>());
                message.Complete();
                if (receivedMessages.Count >= messageCount)
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

        [TestMethod]
        public void Send_Three_Sessions_Should_Reach_Correct_Subscriber()
        {
            var sessionQueue = namespaceManager.CreateQueue(new QueueDescription(Guid.NewGuid().ToString()) { RequiresSession = true });
            var sessionQueueClient = QueueClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, sessionQueue.Path);
            List<string> session1Data = new List<string>() { "Session1-1", "Session1-2", "Session1-3" };
            List<string> session2Data = new List<string>() { "Session2-1", "Session2-2", "Session3-3" };
            List<string> session3Data = new List<string>() { "Session3-1", "Session3-2", "Session3-3" };
            List<string> receivedSession1Data = new List<string>();
            List<string> receivedSession2Data = new List<string>();
            List<string> receivedSession3Data = new List<string>();
            ManualResetEvent session1WaitEvent = new ManualResetEvent(false);
            ManualResetEvent session2WaitEvent = new ManualResetEvent(false);
            ManualResetEvent session3WaitEvent = new ManualResetEvent(false);

            string sessionId1 = Guid.NewGuid().ToString();
            string sessionId2 = Guid.NewGuid().ToString();
            string sessionId3 = Guid.NewGuid().ToString();

            var messageSession1 = sessionQueueClient.AcceptMessageSession(sessionId1);
            messageSession1.OnMessage((message)=> {
                receivedSession1Data.Add(message.GetBody<string>());
                if(receivedSession1Data.Count >= session1Data.Count)
                {
                    session1WaitEvent.Set();
                }
            }, new OnMessageOptions());

            var messageSession2 = sessionQueueClient.AcceptMessageSession(sessionId2);
            messageSession2.OnMessage((message) => {
                receivedSession2Data.Add(message.GetBody<string>());
                if (receivedSession2Data.Count >= session2Data.Count)
                {
                    session2WaitEvent.Set();
                }
            }, new OnMessageOptions());

            var messageSession3 = sessionQueueClient.AcceptMessageSession(sessionId3);
            messageSession3.OnMessage((message) => {
                receivedSession3Data.Add(message.GetBody<string>());
                if (receivedSession3Data.Count >= session3Data.Count)
                {
                    session3WaitEvent.Set();
                }
            }, new OnMessageOptions());

            for (int i = 0; i < session1Data.Count; i++)
            {
                sessionQueueClient.Send(new BrokeredMessage(session1Data[i]) { SessionId = sessionId1 });
                sessionQueueClient.Send(new BrokeredMessage(session2Data[i]) { SessionId = sessionId2 });
                sessionQueueClient.Send(new BrokeredMessage(session3Data[i]) { SessionId = sessionId3 });
            }
            
            session1WaitEvent.WaitOne(5000);
            session2WaitEvent.WaitOne(5000);
            session3WaitEvent.WaitOne(5000);

            bool session1DataReceived = Enumerable.SequenceEqual(session1Data, receivedSession1Data);
            bool session2DataReceived = Enumerable.SequenceEqual(session2Data, receivedSession2Data);
            bool session3DataReceived = Enumerable.SequenceEqual(session3Data, receivedSession3Data);

            if (sessionQueue != null)
            {
                namespaceManager.DeleteQueue(sessionQueue.Path);
            }

            Assert.IsTrue(session1DataReceived && session2DataReceived && session3DataReceived, "When sending interleaved data on session queue, data is not correctly received in OnMessage handlers.");
        }

        #endregion

    }
}
