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
        static async Task<bool> RunAsyncTests(SmokeDb db)
        {
            Console.Write("Awaiting usp_GetItems - ");
            IEnumerable<Item> items = await db.Database.Connection.Call(timeout).usp_GetItems();
            if (!TestGetItemsResults(items))
                return false;

            Console.Write("Awaiting usp_GetItem (WithParameter) - ");

            items = await db.Database.Connection.Call(timeout).usp_GetItem(ItemId: 0);
            if (!TestGetItemResults(items))
                return false;

            Console.Write("Awaiting usp_GetSpokes (No parameters) - ");

            IEnumerable<int> spokes = await db.Database.Connection.Call(timeout).usp_GetSpokes();
            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Awaitinging usp_GetSpokes (WithParameter) - ");

            spokes = db.Database.Connection.Call(timeout).usp_GetSpokes(minimumSpokes: 4);

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Awaiting usp_GetSpokes (WithInput) - ");

            spokes = db.Database.Connection.Call(timeout).usp_GetSpokes(new { minimumSpokes = 6 });

            if (!spokes.SequenceEqual(new[] { 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Awaiting usp_GetSpokes (WithParameter expecting no results) - ");

            spokes = db.Database.Connection.Call(timeout).usp_GetSpokes(minimumSpokes: 24);

            if (!spokes.Any())
                WriteSuccess();
            else
            {
                WriteError("\t" + spokes.Count() + " spokes returned");
                return false;
            }

            Console.Write("Awaiting usp_GetSpokes (Enum result) (No parameters) - ");

            IEnumerable<Spoke> spokes2 = db.Database.Connection.Call(timeout).usp_GetSpokes();

            if (!spokes2.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Awaiting usp_GetSpokes (Enum result) (WithParameter) - ");

            spokes2 = db.Database.Connection.Call(timeout).usp_GetSpokes(minimumSpokes: 4);

            if (!spokes2.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Awaiting usp_GetSpokes (Enum result) (WithInput) - ");

            spokes2 = db.Database.Connection.Call(timeout).usp_GetSpokes(new { minimumSpokes = 6 });

            if (!spokes2.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Awaiting usp_GetSpokes (Enum result) (WithParameter expecting no results) - ");

            spokes2 = db.GetSpokes2
                        .WithParameter("minimumSpokes", 24)
                        .Execute(db.Database.Connection, timeout);

            if (!spokes2.Any())
                WriteSuccess();
            else
            {
                WriteError("\t" + spokes2.Count() + " spokes returned");
                return false;
            }

            Console.Write("Awaiting usp_GetWidget (WithParameter) - ");

            Tuple<IEnumerable<Widget>, IEnumerable<WidgetComponent>> widgets =
                db.Database.Connection.Call(timeout).usp_GetWidget(WidgetId: 1);

            if (!TestGetWidgetResults(widgets))
                return false;

            Console.Write("Awaiting usp_GetWidget (WithInput) - ");
            widgets = db.Database.Connection.Call(timeout).usp_GetWidget(new { WidgetId = 1 });

            if (!TestGetWidgetResults(widgets))
                return false;

            Console.Write("Awaiting usp_ReturnsOne (With Input argument class) - ");

            var ro = new ReturnsOne();
            await db.Database.Connection.Call(timeout).usp_ReturnsOne(ro);
            if (ro.ReturnValue != 1)
            {
                WriteError("\tusp_ReturnsOne did not set the ReturnValue for WithInput");
                return false;
            }

            WriteSuccess();

            Console.Write("Awaiting usp_ReturnsOne (With out parameter) - ");
            int retVal;
            try
            {
                await db.Database.Connection.Call(timeout).usp_ReturnsOne(ReturnValue: out retVal);
                WriteError("\tawaiting with an out parameter should have thrown an exception.");
                return false;
            }
            catch (NotSupportedException)
            {
                WriteSuccess();
            }

            Console.Write("Awaitng usp_GetExistingPeople - ");

            var pIn = new[] 
            {
                new Person { FirstName = "John", LastName = "Doe" },
                new Person { FirstName = "Jane", LastName = "Doe" }
            };
            IEnumerable<Person> ppl = await db.Database.Connection.Call(timeout).usp_GetExistingPeople(people: pIn);
            if (!TestGetExistingPeopleResults(ppl))
                return false;

            Console.Write("Awaiting usp_GetExistingPeople (With Input No Attribute) - ");

            ppl = await db.Database.Connection.Call(timeout).usp_GetExistingPeople(new { people = pIn });
            if (!TestGetExistingPeopleResults(ppl))
                return false;

            Console.Write("Awaiting usp_GetExistingPeople (With Input With Attribute) - ");

            ppl = await db.Database.Connection.Call(timeout).usp_GetExistingPeople(new PersonInput { People = pIn });
            if (!TestGetExistingPeopleResults(ppl))
                return false;

            return true;
        }
    }
}
