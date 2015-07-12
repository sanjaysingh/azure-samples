using Microsoft.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;
using System.Threading;

namespace ServiceBusTest
{
    [TestClass]
    public class ServiceBusTopicTests
    {
        #region private fields

        private TopicDescription testTopic;
        private static NamespaceManager namespaceManager;
        private TopicClient topicClient;

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
            string topicName = Guid.NewGuid().ToString();
            testTopic = namespaceManager.CreateTopic(topicName);
            topicClient = TopicClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, topicName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            namespaceManager.DeleteTopic(testTopic.Path);
        }

        #endregion

        #region tests

        [TestMethod]
        public void Send_Message_To_Topic_No_Filter_Generic_Subscriber_Should_Receive()
        {
            var subscription = namespaceManager.CreateSubscription(testTopic.Path, "All");
            string messageSent = "Sample topic message";
            topicClient.Send(new BrokeredMessage(messageSent));

            SubscriptionClient subscriptionClient = SubscriptionClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString,testTopic.Path,"All");

            var receivedMessage = subscriptionClient.Receive(TimeSpan.FromSeconds(5));

            Assert.AreEqual(messageSent, receivedMessage.GetBody<string>(), "Topice subscriber with no filter did not get the message as expected.");
        }

        [TestMethod]
        public void Send_Message_To_Topic_With_Specific_Filter_Specific_Subscriber_Should_Receive()
        {
            string messageSent = "A policy updated message";

            SqlFilter messageFilter = new SqlFilter("MessageName = 'PolicyUpdated'");

            var subscription = namespaceManager.CreateSubscription(testTopic.Path, "PolicyUpdated", messageFilter);

            var brokeredMessage = new BrokeredMessage(messageSent);
            brokeredMessage.Properties["MessageName"] = "PolicyUpdated";
            topicClient.Send(brokeredMessage);

            SubscriptionClient subscriptionClient = SubscriptionClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, testTopic.Path, "PolicyUpdated");

            var receivedMessage = subscriptionClient.Receive(TimeSpan.FromSeconds(5));

            Assert.AreEqual(messageSent, receivedMessage.GetBody<string>(), "Topice subscriber with specific filter did not get the message as expected.");
        }

        [TestMethod]
        public void Send_Message_To_Topic_Filtered_Subscription_Adds_Extra_Properties_Receiver_Should_Get_All_Properties()
        {
            string messageSent = "A policy updated message";

            var rdHiPriority = new RuleDescription("HighPriority");
            rdHiPriority.Filter = new SqlFilter("MessageName = 'PolicyUpdated'");
            rdHiPriority.Action = new SqlRuleAction("SET BackColor='Red'");

            var subscription = namespaceManager.CreateSubscription(testTopic.Path, "PolicyUpdated", rdHiPriority);
                        
            SubscriptionClient subscriptionClient = SubscriptionClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, testTopic.Path, "PolicyUpdated");
                        
            var brokeredMessage = new BrokeredMessage(messageSent);
            brokeredMessage.Properties["MessageName"] = "PolicyUpdated";
            topicClient.Send(brokeredMessage);

            var receivedMessage = subscriptionClient.Receive(TimeSpan.FromSeconds(5));

            Assert.IsTrue(receivedMessage.Properties["BackColor"].ToString() == "Red" && receivedMessage.Properties["MessageName"].ToString() == "PolicyUpdated", "In topic subscription, properties updated in message was not received correctly.");
        }

        [TestMethod]
        public void Send_Filtered_Message_Multiple_Subscriber_Should_Reach_Expected_Ones()
        {
            string nonFlaggedMessage = "This is message with no flagging";
            string redFlaggedMessage = "This is red flagged message";
            string greenFlaggedMessage = "This is green flagged message";

            List<string> allFlagReceiverMessages = new List<string>();
            List<string> redFlagReceiverMessages = new List<string>();
            List<string> greenFlagReceiverMessages = new List<string>();

            CountdownEvent waitEvent = new CountdownEvent(5);

            namespaceManager.CreateSubscription(testTopic.Path, "ProcessAllMessage");
            namespaceManager.CreateSubscription(testTopic.Path, "ProcessRedMessage", new SqlFilter("Flag = 'Red'"));
            namespaceManager.CreateSubscription(testTopic.Path, "ProcessGreenMessage", new SqlFilter("Flag = 'Green'"));

            SubscriptionClient allSubscriptionClient = SubscriptionClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, testTopic.Path, "ProcessAllMessage");
            allSubscriptionClient.OnMessage((message) => {
                allFlagReceiverMessages.Add(message.GetBody<string>());
                message.Complete();
                waitEvent.Signal();
            });

            SubscriptionClient redSubscriptionClient = SubscriptionClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, testTopic.Path, "ProcessRedMessage");
            redSubscriptionClient.OnMessage((message) => {
                redFlagReceiverMessages.Add(message.GetBody<string>());
                message.Complete();
                waitEvent.Signal();
            });

            SubscriptionClient greenSubscriptionClient = SubscriptionClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, testTopic.Path, "ProcessGreenMessage");
            greenSubscriptionClient.OnMessage((message) => {
                greenFlagReceiverMessages.Add(message.GetBody<string>());
                message.Complete();
                waitEvent.Signal();
            });

            var nonFlaggedBrokeredMessage = new BrokeredMessage(nonFlaggedMessage);
            topicClient.Send(nonFlaggedBrokeredMessage);

            var redFlaggedBrokeredMessage = new BrokeredMessage(redFlaggedMessage);
            redFlaggedBrokeredMessage.Properties["Flag"] = "Red";
            topicClient.Send(redFlaggedBrokeredMessage);

            var greenFlaggedBrokeredMessage = new BrokeredMessage(greenFlaggedMessage);
            greenFlaggedBrokeredMessage.Properties["Flag"] = "Green";
            topicClient.Send(greenFlaggedBrokeredMessage);

            waitEvent.Wait(TimeSpan.FromSeconds(10));

            bool allMessageReceived = allFlagReceiverMessages.Contains(nonFlaggedMessage) && allFlagReceiverMessages.Contains(redFlaggedMessage) && allFlagReceiverMessages.Contains(greenFlaggedMessage);
            Assert.IsTrue(allMessageReceived, "When sending mix of unflitered and filtered messages on a topic, generic subscription did not receieve all the messages.");

            bool redFlagMessageReceived = redFlagReceiverMessages.Contains(redFlaggedMessage);
            bool greenFlagMessageReceived = greenFlagReceiverMessages.Contains(greenFlaggedMessage);

            Assert.IsTrue(redFlagMessageReceived && greenFlagMessageReceived, "When sending mix of filtered and unfiltered messages, specific subsciber did not all coorectly got messages.");

        }

        public void Send_Message_No_Subscriber_Message_Is_Received_When_Subscribed()
        {
            string messageSent = "Sample topic message";
            topicClient.Send(new BrokeredMessage(messageSent));

            var subscription = namespaceManager.CreateSubscription(testTopic.Path, "All");
            SubscriptionClient subscriptionClient = SubscriptionClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, testTopic.Path, "All");

            var receivedMessage = subscriptionClient.Receive(TimeSpan.FromSeconds(5));

            Assert.AreEqual(messageSent, receivedMessage.GetBody<string>(), "Topic message is not receievd if subscription is created after message was published.");
        }
        #endregion
    }
}
