using Microsoft.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.ServiceBus.Messaging;

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

        #endregion
    }
}
