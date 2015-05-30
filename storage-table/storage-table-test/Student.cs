using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageTableTest
{
    public class Student: TableEntity
    {
        public Student(int year, string id, string firstName, string lastName)
        {
            this.PartitionKey = year.ToString();
            this.RowKey = id;
            this.FirstName = firstName;
            this.LastName = lastName;
        }

        public Student() { }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
    
}
