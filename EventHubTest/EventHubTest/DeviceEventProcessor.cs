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
    public class DeviceEventProcessor : IEventProcessor
    {
        PartitionContext partitionContext;
        
        public DeviceEventProcessor()
        {
           
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
                TestMediator.Instance.OnReceive(eventData);
            }
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        
    }
}
