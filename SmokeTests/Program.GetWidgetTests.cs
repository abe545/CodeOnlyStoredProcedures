using CodeOnlyStoredProcedure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmokeTests
{
    partial class Program
    {
        static bool DoGetWidgetTests(SmokeDb db)
        {
            Console.Write("Calling usp_GetWidget synchronously (WithParameter) - ");

            var items = db.GetWidget
                          .WithParameter("WidgetId", 1)
                          .Execute(db.Database.Connection, timeout);

            if (!TestGetWidgetResults(items))
                return false;
            
            Console.Write("Calling usp_GetWidget synchronously (WithInput) - ");

            items = db.GetWidget
                      .WithInput(new { WidgetId = 1 })
                      .Execute(db.Database.Connection, timeout);

            if (!TestGetWidgetResults(items))
                return false;

            Console.Write("Calling usp_GetWidget synchronously (Dynamic Syntax) - ");

            items = StoredProcedure.Call(db.Database.Connection, timeout).usp_GetWidget<Widget, WidgetComponent>(WidgetId: 1);

            if (!TestGetWidgetResults(items))
                return false;

            Console.Write("Calling usp_GetWidget asynchronously (WithParameter) - ");

            items = db.GetWidget
                      .WithParameter("WidgetId", 1)
                      .ExecuteAsync(db.Database.Connection, timeout)
                      .Result;

            if (!TestGetWidgetResults(items))
                return false;

            Console.Write("Calling usp_GetWidget asynchronously (WithInput) - ");

            items = db.GetWidget
                      .WithInput(new { WidgetId = 1 })
                      .ExecuteAsync(db.Database.Connection, timeout)
                      .Result;

            if (!TestGetWidgetResults(items))
                return false;

            Console.Write("Calling usp_GetWidget asynchronously (Dynamic Syntax) - ");

            items = StoredProcedure.CallAsync(db.Database.Connection, timeout).usp_GetWidget<Widget, WidgetComponent>(WidgetId: 1).Result;

            if (!TestGetWidgetResults(items))
                return false;

            Console.Write("Calling usp_GetItem asynchronously two times simultaneously - ");

            var sp = db.GetWidget.WithInput(new { WidgetId = 1 });

            var t1 = sp.ExecuteAsync(db.Database.Connection, timeout);
            var t2 = sp.ExecuteAsync(db.Database.Connection, timeout);

            Task.WaitAll(t1, t2);

            if (!TestGetWidgetResults(t1.Result, false))
                return false;

            return TestGetWidgetResults(t2.Result);
        }

        static bool TestGetWidgetResults(Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>> items, bool finalSuccess = true)
        {
            var widget = items.Item1;
            var components = items.Item2;

            if (!widget.Any())
            {
                WriteError("\tDid not return any items in the first result set.");
                return false;
            }
            if (!components.Any())
            {
                WriteError("\tDid not return any items in the second result set.");
                return false;
            }

            if (widget.Count() > 1)
            {
                WriteError("\tUnexpected results returned in the first result set.");
                return false;
            }

            var w = widget.Single();
            if (w.WidgetId != 1 || 
               !w.IsNew.HasValue ||
                w.IsNew.Value ||
                w.Name != "Grub" ||
                w.Price != 22.22M ||
                w.Weight != 3.3)
            {
                WriteError("\tData not mapped correctly in first result set.");
                return false;
            }

            if (components.Count() != 3)
            {
                WriteError("\tUnexpected results returned in the second result set.");
                return false;
            }

            var ids = components.Select(c => c.WidgetComponentId)
                                .OrderBy(i => i);

            if (!ids.SequenceEqual(new[] { 2, 3, 4 }))
            {
                WriteError("\tData not mapped correctly in second result set.");
                return false;
            }

            var names = components.Select(c => c.Name)
                                  .OrderBy(s => s);

            if (!names.SequenceEqual(new[] { "Antennae", "Compound Eye", "Leg" }))
            {
                WriteError("\tData not mapped correctly in second result set.");
                return false;
            }

            if (finalSuccess)
                WriteSuccess();

            return true;
        }
    }
}
