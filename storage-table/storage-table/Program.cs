using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace storage_table
{
    class Program
    {
        static void Main(string[] args)
        {
            TableFunctions tableFunctions = new TableFunctions();

            Console.WriteLine("Creating table..");
            TimedAction(()=>tableFunctions.CreateTable());

            Console.WriteLine("Inserting 1000 rows");
            TimedAction(()=>tableFunctions.BatchInsert(10));

            Console.WriteLine("Performing 1000 selects..");
            TimedAction(()=>tableFunctions.Select(10));

            Console.WriteLine("Deleting table");
            TimedAction(() => tableFunctions.DeleteTable());

            Console.Read();
        }

        private static void TimedAction(Action action)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            action();
            watch.Stop();
            Console.WriteLine("Time Taken = " + watch.Elapsed.TotalSeconds);
        }
    }
}
