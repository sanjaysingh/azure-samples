using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EventHubTest
{
    [TestClass]
    public class EventHubTest
    {
        private static NamespaceManager namespaceManager;
        private EventHubDescription eventHub;
        private EventHubClient client;

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
            string eventHubName = Guid.NewGuid().ToString();
            eventHub = namespaceManager.CreateEventHubIfNotExists(eventHubName);
            
            client = EventHubClient.CreateFromConnectionString(AppSetting.ServiceBusConnectionString, eventHubName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            namespaceManager.DeleteEventHub(eventHub.Path);
        }

        [TestMethod]
        public void Send_Specific_Partition_Receieve_Specific_Partition_Should_Match()
        {
            var messageSent = "Hello Event Hub";
            var sender = client.CreatePartitionedSender(eventHub.PartitionIds[0]);
            sender.Send(new EventData(Encoding.UTF8.GetBytes(messageSent)));

            var group = client.GetDefaultConsumerGroup();
            var receiver = group.CreateReceiver(client.GetRuntimeInformation().PartitionIds[0]);

            var receivedMessage = Encoding.UTF8.GetString(receiver.Receive().GetBytes());

            Assert.AreEqual(messageSent, receivedMessage, "Message sent on event hub was not received correctly");
        }

        [TestMethod]
        public void Send_Multiple_Evenets_One_Consumer_Ensure_All_Events_From_A_Source_In_Order()
        {
            Random random = new Random();
            int numberOfMessages = 1000;
            int numberOfDevices = 50;
            List<Task> senderTasks = new List<Task>();

            Dictionary<string, List<int>> sentTemperatures = new Dictionary<string, List<int>>();
            // send messages
            for (int i = 0; i < numberOfMessages; ++i)
            {
                MetricEvent info = new MetricEvent() { DeviceId = random.Next(numberOfDevices), Temperature = random.Next(100) };
                var serializedString = JsonConvert.SerializeObject(info);
                EventData data = new EventData(Encoding.UTF8.GetBytes(serializedString)) { PartitionKey = info.DeviceId.ToString() };
                senderTasks.Add(client.SendAsync(data));
                if (!sentTemperatures.ContainsKey(data.PartitionKey))
                {
                    sentTemperatures.Add(data.PartitionKey, new List<int>());
                }
                sentTemperatures[data.PartitionKey].Add(info.Temperature);
            }

            var defaultConsumerGroup = client.GetDefaultConsumerGroup();
            var eventProcessorHost = new EventProcessorHost("singleworker", client.Path, defaultConsumerGroup.GroupName, AppSetting.ServiceBusConnectionString, AppSetting.StorageConnectionString);
            eventProcessorHost.RegisterEventProcessorAsync<DeviceEventProcessor>().Wait();
        }

    }
}