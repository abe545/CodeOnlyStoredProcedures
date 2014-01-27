using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmokeTests
{
    partial class Program
    {
        static bool DoGetItemsTests(SmokeDb db)
        {
            Console.Write("Calling usp_GetItems synchronously - ");

            var res = db.GetItems.Execute(db.Database.Connection);
            if (!TestGetItemsResults(res))
                return false;

            Console.Write("Calling usp_GetItems asynchronously - ");
            res = db.GetItems.ExecuteAsync(db.Database.Connection).Result;
            if (!TestGetItemsResults(res))
                return false;

            Console.Write("Calling usp_GetItems two times asynchronoulsy simultaneously - ");
            var t1 = db.GetItems.ExecuteAsync(db.Database.Connection);
            var t2 = db.GetItems.ExecuteAsync(db.Database.Connection);
            Task.WaitAll(t1, t2);
            if (!TestGetItemsResults(t1.Result, false))
                return false;
            if (!TestGetItemsResults(t2.Result, false))
                return false;

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Success!");
            Console.ResetColor();

            return true;
        }

        private static bool TestGetItemsResults(IEnumerable<Item> res, bool finalSuccess = true)
        {
            if (res == null)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("\tNull returned from usp_GetItems.");
                Console.ResetColor();

                return false;
            }

            if (res.Count() != 2)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("\tWrong number of results returned from usp_GetItems.");
                Console.ResetColor();

                return false;
            }

            var item = res.First();
            if (item.ItemId != 0 || item.Name != "Foo")
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("\tIncorrect first item.\n\t\tExpected 'Foo' [0]\n\t\tActual '{0}' [{1}]", item.Name, item.ItemId);
                Console.ResetColor();

                return false;
            }

            item = res.Last();
            if (item.ItemId != 1 || item.Name != "Bar")
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Failed!");
                Console.WriteLine("\tIncorrect first item.\n\t\tExpected 'Bar' [1]\n\t\tActual '{0}' [{1}]", item.Name, item.ItemId);
                Console.ResetColor();

                return false;
            }

            if (finalSuccess)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Success!");
                Console.ResetColor();
            }

            return true;
        }
    }
}
