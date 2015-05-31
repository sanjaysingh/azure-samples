using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System.Threading;

namespace StorageTableTest
{
    [TestClass]
    public class StorageTableTest
    {
        #region fields

        static CloudStorageAccount storageAccount;
        private const string tableName = "TestTable";
        static CloudTableClient tableClient;
        static CloudTable table;
        
        #endregion

        #region inits

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            storageAccount = CloudStorageAccount.Parse(AppSetting.StorageConnectionString);
            tableClient = storageAccount.CreateCloudTableClient();
            
            table = tableClient.GetTableReference(tableName);

            table.CreateIfNotExists();
        }
        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (table != null)
            {
                table.Delete();
            }
        }
        [TestCleanup]
        public void TestCleanup()
        {
            foreach (var entity in GetAllEntities())
            {
                table.Execute(TableOperation.Delete(entity));
            }
        }

        #endregion

        #region tests
        
        [TestMethod]
        public void Insert_One_Entity_Verify_Count()
        {
            var student = new Student(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var insertOperation = TableOperation.Insert(student);

            table.Execute(insertOperation);

            int afterInsertCount = GetAllEntitiesInPartition(Student.EntityPartitionKey).Count();
            
            Assert.IsTrue(afterInsertCount == 1, "A single entity insert did not function as expected.");
        }

        [TestMethod]
        public void Update_One_Entity_Verify_NewValue()
        {
            var student = new Student(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var insertOperation = TableOperation.Insert(student);

            table.Execute(insertOperation);

            var oldFirstName = student.FirstName;
            var newFirstName = Guid.NewGuid().ToString();

            var selectedStudent = GetEntity(student.PartitionKey, student.RowKey);
            selectedStudent.FirstName = newFirstName;
            
            table.Execute(TableOperation.Replace(selectedStudent));

            selectedStudent = GetEntity(student.PartitionKey, student.RowKey);
            
            Assert.IsTrue(selectedStudent.FirstName == newFirstName, "Update of a single entity did not work as expected.");
        }

        [TestMethod]
        public void Batch_Insert_Entities_Verify_Count()
        {
            TableBatchOperation insertBatchOperation = new TableBatchOperation();
            
            int insertCount = 100;
            for (int i = 1; i <= insertCount; i++)
            {
                var student = new Student(i.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
                student.ETag = "*";
                insertBatchOperation.Insert(student);
            }
            
            table.ExecuteBatch(insertBatchOperation);
            int afterInsertCount = GetAllEntitiesInPartition(Student.EntityPartitionKey).Count();
            
            Assert.IsTrue(afterInsertCount == 100, "A batch insert did not function as expected.");
        }

        [TestMethod]
        public void Insert_Thousand_VerifyCount()
        {
            for(int batch = 1; batch <= 10; batch++)
            {
                TableBatchOperation insertBatchOperation = new TableBatchOperation();

                int insertCount = 100;
                for (int i = 1; i <= insertCount; i++)
                {
                    var student = new Student(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
                    insertBatchOperation.Insert(student);
                }
                table.ExecuteBatch(insertBatchOperation);
            }
            int afterInsertCount = GetAllEntitiesInPartition(Student.EntityPartitionKey).Count();
            Assert.IsTrue(afterInsertCount == 1000, "Inserting 1000 entities with a batch of 100 failed.");
        }

        [TestMethod]
        public void Insert_TwoEntityType_Verify_Added()
        {
            var student = new Student(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            
            table.Execute(TableOperation.Insert(student));

            var teacher = new Teacher(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            table.Execute(TableOperation.Insert(teacher));
            
            int afterInsertCount = GetAllEntities().Count();

            Assert.IsTrue(afterInsertCount == 2, "Inserting multiple entity types in a table");
        }
        
        #endregion

        #region private methods

        private static IEnumerable<Student> GetAllEntities()
        {
            TableQuery<Student> partitionQuery = new TableQuery<Student>();

            return table.ExecuteQuery(partitionQuery);
        }

        private static IEnumerable<Student> GetAllEntitiesInPartition(string partition)
        {
            TableQuery<Student> partitionQuery = new TableQuery<Student>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition));

            return table.ExecuteQuery(partitionQuery);
        }

        private static Student GetEntity(string partition, string key)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<Student>(partition, key);
            return table.Execute(retrieveOperation).Result as Student;
        }

        #endregion
    }
}
