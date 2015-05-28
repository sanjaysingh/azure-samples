using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace storage_table
{
    public static class AppSetting
    {
        public static string StorageConnectionString
        {
            get
            {
                return CloudConfigurationManager.GetSetting("StorageConnectionString");
            }
        }
    }
}
