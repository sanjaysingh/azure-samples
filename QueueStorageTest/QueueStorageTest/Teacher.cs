using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueStorageTest
{
    public class Teacher
    {
        

        public Teacher(string id, string firstName, string lastName)
        {
            
            this.Id = id;
            this.FirstName = firstName;
            this.LastName = lastName;
        }

        public Teacher() { }

        public string Id { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Salary { get; set; }

        public int Experience { get; set; }
    }
    
}
