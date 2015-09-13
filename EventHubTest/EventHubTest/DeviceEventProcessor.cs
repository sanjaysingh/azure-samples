using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubTest
{
    public class DeviceEventProcessor : IEventProcessor
    {
        IDictionary<string, List<int>> map;
        PartitionContext partitionContext;
        
        public DeviceEventProcessor()
        {
            this.map = new Dictionary<string, List<int>>();
        }

        public Task OpenAsync(PartitionContext context)
        {
            this.partitionContext = context;
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> events)
        {
            foreach (EventData eventData in events)
            {
                List<int> data;
                var newData = this.DeserializeEventData(eventData);
                string key = eventData.PartitionKey;

                // Name of device generating the event acts as hash key to retrieve average computed for it so far
                if (!this.map.TryGetValue(key, out data))
                {
                    // If this is the first time we got data for this device then generate new state
                    this.map.Add(key, new List<int>());
                }

                // Update data
                this.map[key].Add(newData.Temperature);
                await context.CheckpointAsync();
            }
            
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        MetricEvent DeserializeEventData(EventData eventData)
        {
            return JsonConvert.DeserializeObject<MetricEvent>(Encoding.UTF8.GetString(eventData.GetBytes()));
        }
    }
}
