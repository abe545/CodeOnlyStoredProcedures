using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;

namespace SmokeTests
{
    partial class Program
    {
        static bool DoGetItemsTests(SmokeDb db)
        {
            Console.Write("Calling usp_GetItems synchronously - ");

            var res = db.GetItems.Execute(db.Database.Connection, timeout);
            if (!TestGetItemsResults(res))
                return false;

            Console.Write("Calling usp_GetItems asynchronously - ");
            res = db.GetItems.ExecuteAsync(db.Database.Connection, timeout).Result;
            if (!TestGetItemsResults(res))
                return false;

            Console.Write("Calling usp_GetItems synchronously (Dynamic Syntax) - ");
            res = StoredProcedure.Call(db.Database.Connection, timeout).usp_GetItems<Item>();
            if (!TestGetItemsResults(res))
                return false;

            Console.Write("Calling usp_GetItems asynchronously (Dynamic Syntax) - ");
            res = StoredProcedure.CallAsync(db.Database.Connection, timeout).usp_GetItems<Item>().Result;
            if (!TestGetItemsResults(res))
                return false;

            Console.Write("Calling usp_GetItems two times asynchronoulsy simultaneously - ");
            var t1 = db.GetItems.ExecuteAsync(db.Database.Connection, timeout);
            var t2 = db.GetItems.ExecuteAsync(db.Database.Connection, timeout);
            Task.WaitAll(t1, t2);
            if (!TestGetItemsResults(t1.Result, false))
                return false;
            if (!TestGetItemsResults(t2.Result, false))
                return false;

            WriteSuccess();

            return true;
        }

        private static bool TestGetItemsResults(IEnumerable<Item> res, bool finalSuccess = true)
        {
            if (res == null)
            {
                WriteError("\tNull returned from usp_GetItems.");
                return false;
            }

            if (res.Count() != 2)
            {
                WriteError("\tWrong number of results returned from usp_GetItems.");
                return false;
            }

            var item = res.First();
            if (item.ItemId != 0 || item.Name != "Foo")
            {
                WriteError(string.Format("\tIncorrect first item.\n\t\tExpected 'Foo' [0]\n\t\tActual '{0}' [{1}]", item.Name, item.ItemId));
                return false;
            }

            item = res.Last();
            if (item.ItemId != 1 || item.Name != "Bar")
            {
                WriteError(string.Format("\tIncorrect first item.\n\t\tExpected 'Bar' [1]\n\t\tActual '{0}' [{1}]", item.Name, item.ItemId));
                return false;
            }

            if (finalSuccess)
                WriteSuccess();

            return true;
        }
    }
}
