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
        static bool DoGetExistingPeopleTests(SmokeDb db)
        {
            var toTest = new[] 
            {
                new Person { FirstName = "John", LastName = "Doe" },
                new Person { FirstName = "Jane", LastName = "Doe" }
            };

            Console.Write("Calling usp_GetExistingPeople synchronously - ");

            var res = db.GetExistingPeople
                        .WithTableValuedParameter("people", toTest, "dbo.Person")
                        .Execute(db.Database.Connection, timeout);
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople asynchronously - ");

            res = db.GetExistingPeople
                    .WithTableValuedParameter("people", toTest, "dbo.Person")
                    .ExecuteAsync(db.Database.Connection, timeout)
                    .Result;
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople synchronously (With Input No Attribute) - ");

            res = db.GetExistingPeople
                    .WithInput(new { people = toTest})
                    .Execute(db.Database.Connection, timeout);
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople asynchronously (With Input No Attribute) - ");

            res = db.GetExistingPeople
                    .WithInput(new { people = toTest })
                    .ExecuteAsync(db.Database.Connection, timeout)
                    .Result;
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople synchronously (With Input And Attribute) - ");

            res = db.GetExistingPeople
                    .WithInput(new PersonInput { People = toTest })
                    .Execute(db.Database.Connection, timeout);
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople asynchronously (With Input And Attribute) - ");

            res = db.GetExistingPeople
                    .WithInput(new PersonInput { People = toTest })
                    .ExecuteAsync(db.Database.Connection, timeout)
                    .Result;
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople synchronously (dynamic syntax) - ");

            res = db.Database.Connection.Call(timeout).usp_GetExistingPeople(people: toTest);
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople asynchronously (dynamic syntax) - ");

            Task<IEnumerable<Person>> task =
                db.Database.Connection.Call(timeout).usp_GetExistingPeople(people: toTest);
            res = task.Result;
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople synchronously (dynamic syntax) (With Input No Attribute)  - ");

            res = db.Database.Connection.Call(timeout).usp_GetExistingPeople(new { people = toTest });
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople asynchronously (dynamic syntax)(With Input No Attribute)  - ");

            task = db.Database.Connection.Call(timeout).usp_GetExistingPeople(new { people = toTest });
            res = task.Result;
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople synchronously (dynamic syntax) (With Input And Attribute)  - ");

            res = db.Database.Connection.Call(timeout).usp_GetExistingPeople(new PersonInput { People = toTest });
            if (!TestGetExistingPeopleResults(res))
                return false;

            Console.Write("Calling usp_GetExistingPeople asynchronously (dynamic syntax)(With Input And Attribute)  - ");

            task = db.Database.Connection.Call(timeout).usp_GetExistingPeople(new PersonInput { People = toTest });
            res = task.Result;
            if (!TestGetExistingPeopleResults(res))
                return false;

            return true;
        }

        private static bool TestGetExistingPeopleResults(IEnumerable<Person> res)
        {
            if (res == null)
            {
                WriteError("\tNull returned from usp_GetExistingPeople.");
                return false;
            }

            if (res.Count() != 2)
            {
                WriteError("\tWrong number of results returned from usp_GetExistingPeople.");
                return false;
            }

            var item = res.First();
            if (item.FirstName != "John" || item.LastName != "Doe")
            {
                WriteError(string.Format("\tIncorrect first item.\n\t\tExpected 'John Doe' \n\t\tActual '{0} {1}'", item.FirstName, item.LastName));
                return false;
            }

            item = res.Last();
            if (item.FirstName != "Jane" || item.LastName != "Doe")
            {
                WriteError(string.Format("\tIncorrect last item.\n\t\tExpected 'Jane Doe' \n\t\tActual '{0} {1}'", item.FirstName, item.LastName));
                return false;
            }

            WriteSuccess();

            return true;
        }

        private class PersonInput
        {
            [TableValuedParameter(TableName = "Person", Name = "people")]
            public IEnumerable<Person> People { get; set; }
        }
    }
}
