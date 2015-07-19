using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBusTest
{
    public static class AppSetting
    {
        public const string Namespace = "sanjaygeicons";
        public const string RootAccessKey = "ef7ytYlkXvCCKGFFhaCFhrAx4SFob6r2GG9oBYPfBqI=";
        public static readonly string ServiceBusConnectionString = $"Endpoint=sb://{AppSetting.Namespace}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={AppSetting.RootAccessKey}";
    }
}
