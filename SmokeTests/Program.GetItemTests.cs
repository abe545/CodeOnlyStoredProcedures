using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmokeTests
{
    partial class Program
    {
        static bool DoGetItemTests(SmokeDb db)
        {
            Console.Write("Calling usp_GetItem synchronously (WithParameter) - ");

            var items = db.GetItem
                          .WithParameter("ItemId", 0)
                          .Execute(db.Database.Connection);

            if (!TestGetItemResults(items))
                return false;
            
            Console.Write("Calling usp_GetItem synchronously (WithInput) - ");

            items = db.GetItem
                      .WithInput(new GetItemInput { Id = 0 })
                      .Execute(db.Database.Connection);

            if (!TestGetItemResults(items))
                return false;

            Console.Write("Calling usp_GetItem asynchronously (WithParameter) - ");

            items = db.GetItem
                          .WithParameter("ItemId", 0)
                          .ExecuteAsync(db.Database.Connection)
                          .Result;

            if (!TestGetItemResults(items))
                return false;

            Console.Write("Calling usp_GetItem asynchronously (WithInput) - ");

            items = db.GetItem
                      .WithInput(new GetItemInput { Id = 0 })
                      .ExecuteAsync(db.Database.Connection)
                      .Result;

            if (!TestGetItemResults(items))
                return false;

            Console.Write("Calling usp_GetItem asynchronously two times simultaneously - ");

            var sp = db.GetItem.WithInput(new { ItemId = 0 });

            var t1 = sp.ExecuteAsync(db.Database.Connection);
            var t2 = sp.ExecuteAsync(db.Database.Connection);

            Task.WaitAll(t1, t2);

            if (!TestGetItemResults(t1.Result, false))
                return false;

            return TestGetItemResults(t2.Result);
        }

        private static bool TestGetItemResults(IEnumerable<Item> items, bool writeSuccess = true)
        {
            if (items.Count() != 1)
            {
                WriteError("\tusp_GetItem returned more than one item");
                return false;
            }

            var item = items.Single();
            if (item.ItemId != 0 || item.Name != "Foo")
            {
                WriteError("\tusp_GetItem returned the wrong item");
                return false;
            }

            if (writeSuccess)
                WriteSuccess();

            return true;
        }

        private class GetItemInput
        {
            [StoredProcedureParameter(Name = "ItemId")]
            public int Id { get; set; }
        }
    }
}
