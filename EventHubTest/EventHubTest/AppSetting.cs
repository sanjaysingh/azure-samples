using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubTest
{
    public static class AppSetting
    {
        public const string Namespace = "";
        public const string RootAccessKey = "";
        public const string StorageAccountName = "";
        public const string StorageAccountKey = "+EPw2BdOjYEdsdr6XKmzwLTgfCgB4Ieb5P7egAp2sDJJK1zrmsldkiQ==";
        public static readonly string ServiceBusConnectionString = $"Endpoint=sb://{AppSetting.Namespace}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={AppSetting.RootAccessKey}";
        public static readonly string StorageConnectionString = $"AccountName={StorageAccountName};AccountKey={StorageAccountKey};DefaultEndpointsProtocol=https";
    }
}
