using CodeOnlyStoredProcedure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SmokeTests
{
    partial class Program
    {

        static bool DoReturnsOneTests(SmokeDb db)
        {
            Console.Write("Calling usp_ReturnsOne synchronously - ");

            var ro = new ReturnsOne();
            db.ReturnsOne.WithInput(ro).Execute(db.Database.Connection);
            if (ro.ReturnValue != 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("\tusp_ReturnsOne did not set the ReturnValue for WithInput");
                Console.ResetColor();
                return false;
            }

            int res = -1;

            db.ReturnsOne.WithReturnValue(i => res = i)
                         .Execute(db.Database.Connection);
            if (res != 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("\tusp_ReturnsOne did not set the ReturnValue for WithReturnValue");
                Console.ResetColor();
                return false;
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Success!");
            Console.ResetColor();

            Console.Write("Calling usp_ReturnsOne asynchronously - ");

            ro = new ReturnsOne();
            db.ReturnsOne.WithInput(ro)
                         .ExecuteAsync(db.Database.Connection)
                         .Wait();
            if (ro.ReturnValue != 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("\tusp_ReturnsOne did not set the ReturnValue for WithInput");
                Console.ResetColor();

                return false;
            }

            res = -1;

            db.ReturnsOne.WithReturnValue(i => res = i)
                         .ExecuteAsync(db.Database.Connection)
                         .Wait();
            if (res != 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("usp_ReturnsOne did not set the ReturnValue for WithReturnValue");
                Console.ResetColor();

                return false;
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Success!");
            Console.ResetColor();

            Console.Write("Calling usp_ReturnsOne two times asynchronously simultaneously - ");

            var results = new List<int>();
            var sp = db.ReturnsOne.WithReturnValue(i => results.Add(i));

            var t1 = sp.ExecuteAsync(db.Database.Connection);
            var t2 = sp.ExecuteAsync(db.Database.Connection);

            Task.WaitAll(t1, t2);

            if (results.Count != 2 || results.Any(i => i != 1))
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("\tBoth stored procedures did not return 1.");
                for (int i = 0; i < results.Count; ++i)
                    Console.WriteLine("\t\tResult {0} - {1}", i, results[i]);
                Console.ResetColor();

                return false;
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Success!");
            Console.ResetColor();

            return true;
        }

        private class ReturnsOne
        {
            [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
            public int ReturnValue { get; set; }
        }
    }
}
