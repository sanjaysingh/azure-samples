using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBusTest
{
    public class Student
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public override int GetHashCode()
        {
            return this.Age + this.Id.GetHashCode() + this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var secondStudent = obj as Student;
            if (secondStudent == null) return false;
            if (secondStudent == this) return true;

            return this.Id == secondStudent.Id && this.Name == secondStudent.Name && this.Age == secondStudent.Age;
        }
    }
}
