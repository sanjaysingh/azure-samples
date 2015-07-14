using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBusTest
{
    public static class AppSetting
    {
        public const string Namespace = "";
        public const string RootAccessKey = "";
        public static readonly string ServiceBusConnectionString = $"Endpoint=sb://{AppSetting.Namespace}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={AppSetting.RootAccessKey}";
    }
}
