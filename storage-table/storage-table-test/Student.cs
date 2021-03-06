﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageTableTest
{
    public class Student: TableEntity
    {
        public const string EntityPartitionKey = "Student";
        public Student(string id, string firstName, string lastName)
        {
            this.PartitionKey = EntityPartitionKey;
            this.RowKey = id;
            this.FirstName = firstName;
            this.LastName = lastName;
        }

        public Student() { }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
    
}
