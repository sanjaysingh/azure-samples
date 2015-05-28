using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace storage_table
{
    public class TableFunctions
    {
        private const string TableName = "Person";
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(AppSetting.StorageConnectionString);

        public void CreateTable()
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(TableName);
            table.CreateIfNotExists();
            
        }

        public void DeleteTable()
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(TableName);
            table.Delete();
        }

        public void BatchInsert(int count)
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            
            CloudTable table = tableClient.GetTableReference(TableName);

            TableBatchOperation batchOperation = new TableBatchOperation();

            for(int i = 1; i <= count; i++)
            {
                batchOperation.Insert(new Student(2015, i.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            }
            // Execute the batch operation.
            table.ExecuteBatch(batchOperation);
        }

        public void Select(int count)
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference(TableName);

            for (int i = 1; i <= count; i++)
            {
                TableOperation retrieveOperation = TableOperation.Retrieve<Student>("2015", i.ToString());

                // Execute the retrieve operation.
                TableResult retrievedResult = table.Execute(retrieveOperation);
            }
            
        }
    }
}
