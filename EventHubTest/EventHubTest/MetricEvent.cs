using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EventHubTest
{
    [DataContract]
    public class MetricEvent
    {
        [DataMember]
        public int DeviceId { get; set; }
        [DataMember]
        public int Temperature { get; set; }
    }
}
