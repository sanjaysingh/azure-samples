namespace FileShareStorageTest
{
    public static class AppSetting
    {
        public static string StorageAccountName = "";
        public static string StorageAccountKey = "";
        public static string StorageConnectionString = $"DefaultEndpointsProtocol=https;AccountName={StorageAccountName};AccountKey={StorageAccountKey}";
        public static string StorageAccountFullName = $"{StorageAccountName}.file.core.windows.net";
    }
}
