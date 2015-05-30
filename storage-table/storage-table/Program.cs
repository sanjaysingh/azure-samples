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
            // does not support more than 100 in a batch as of 5/29/2015
            Console.WriteLine("Inserting 100 rows");
            TimedAction(()=>tableFunctions.BatchInsert(100));

            Console.WriteLine("Inserting 1 row");
            TimedAction(() => tableFunctions.SingleInsert());

            Console.WriteLine("Performing 100 selects..");
            TimedAction(()=>tableFunctions.Select(100));

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
