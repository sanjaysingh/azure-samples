using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventHubTest
{
    public class TestMediator
    {
        private static TestMediator instance;

        public IDictionary<string, List<int>> ReceivedData = new Dictionary<string, List<int>>();
        public const int NumberOfMessages = 50;
        private int numberOfMessagesReceived = 0;
        public ManualResetEvent WaitEvent = new ManualResetEvent(false);

        MetricEvent DeserializeEventData(EventData eventData)
        {
            return JsonConvert.DeserializeObject<MetricEvent>(Encoding.UTF8.GetString(eventData.GetBytes()));
        }

        public static void Initialize()
        {
            instance = new TestMediator();
        }
        public static TestMediator Instance
        {
            get
            {
                return instance;
            }
        }
        public  void OnReceive(EventData eventData)
        {
            lock (ReceivedData)
            {
                var newData = DeserializeEventData(eventData);
                List<int> data;
                string key = eventData.PartitionKey;
                if (!ReceivedData.TryGetValue(key, out data))
                {
                    ReceivedData.Add(key, new List<int>());
                }
                ReceivedData[key].Add(newData.Temperature);
                numberOfMessagesReceived++;
                if (numberOfMessagesReceived == NumberOfMessages)
                {
                    WaitEvent.Set();
                }
            }
        }
    }
}
