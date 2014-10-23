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
        static bool DoGetSpokesTests(SmokeDb db)
        {
            Console.Write("Calling usp_GetSpokes synchronously (No parameters) - ");

            var spokes = db.GetSpokes
                           .Execute(db.Database.Connection, timeout);

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes asynchronously (Dynamic Syntax no parameters) - ");

            spokes = db.Database.Connection.Execute(timeout).usp_GetSpokes<int>();

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes synchronously (WithParameter) - ");

            spokes = db.GetSpokes
                       .WithParameter("minimumSpokes", 4)
                       .Execute(db.Database.Connection, timeout);

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes synchronously (WithInput) - ");

            spokes = db.GetSpokes
                       .WithInput(new { minimumSpokes = 6 })
                       .Execute(db.Database.Connection, timeout);

            if (!spokes.SequenceEqual(new[] { 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes asynchronously (WithParameter) - ");

            spokes = db.GetSpokes
                       .WithParameter("minimumSpokes", 4)
                       .ExecuteAsync(db.Database.Connection, timeout)
                       .Result;

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes asynchronously (Dynamic Syntax) - ");

            spokes = db.Database.Connection.Execute(timeout).usp_GetSpokes(minimumSpokes: 4);

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes asynchronously (WithInput) - ");

            spokes = db.GetSpokes
                       .WithInput(new { minimumSpokes = 6 })
                       .ExecuteAsync(db.Database.Connection, timeout)
                       .Result;

            if (!spokes.SequenceEqual(new[] { 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes asynchronously (Dynamic Syntax) - ");

            Task<IEnumerable<int>> asyncSpokes = db.Database.Connection.ExecuteAsync(timeout).usp_GetSpokes(minimumSpokes: 6);
            spokes = asyncSpokes.Result;

            if (!spokes.SequenceEqual(new[] { 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes synchronously (WithParameter expecting no results) - ");

            spokes = db.GetSpokes
                       .WithParameter("minimumSpokes", 24)
                       .Execute(db.Database.Connection, timeout);

            if (!spokes.Any())
                WriteSuccess();
            else
            {
                WriteError("\t" + spokes.Count() + " spokes returned");
                return false;
            }

            Console.Write("Calling usp_GetSpokes synchronously (Dynamic Syntax expecting no results) - ");

            spokes = db.Database.Connection.Execute(timeout).usp_GetSpokes<int>(minimumSpokes: 100);

            if (!spokes.Any())
                WriteSuccess();
            else
            {
                WriteError("\t" + spokes.Count() + " spokes returned");
                return false;
            }

            Console.Write("Calling usp_GetSpokes (Enum result) synchronously (No parameters) - ");

            var spokes2 = db.GetSpokes2
                            .Execute(db.Database.Connection, timeout);

            if (!spokes2.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes (Enum result) synchronously (Dynamic Syntax no parameters) - ");

            spokes2 = db.Database.Connection.Execute(timeout).usp_GetSpokes<Spoke>();

            if (!spokes2.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes (Enum result) synchronously (WithParameter) - ");

            spokes2 = db.GetSpokes2
                        .WithParameter("minimumSpokes", 4)
                        .Execute(db.Database.Connection, timeout);

            if (!spokes2.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes (Enum result) synchronously (WithInput) - ");

            spokes2 = db.GetSpokes2
                        .WithInput(new { minimumSpokes = 6 })
                        .Execute(db.Database.Connection, timeout);

            if (!spokes2.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes (Enum result) synchronously (Dynamic Syntax) - ");

            spokes2 = db.Database.Connection.Execute(timeout).usp_GetSpokes<Spoke>(minimumSpokes: 6);

            if (!spokes2.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes (Enum result) asynchronously (WithParameter) - ");

            spokes2 = db.GetSpokes2
                        .WithParameter("minimumSpokes", 4)
                        .ExecuteAsync(db.Database.Connection, timeout)
                        .Result;

            if (!spokes2.SequenceEqual(new[] { Spoke.Four, Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes (Enum result) asynchronously (WithInput) - ");

            spokes2 = db.GetSpokes2
                        .WithInput(new { minimumSpokes = 6 })
                        .ExecuteAsync(db.Database.Connection, timeout)
                        .Result;

            if (!spokes2.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes (Enum result) asynchronously (Dynamic Syntax) - ");

            Task<IEnumerable<Spoke>> asyncSpokes2 = db.Database.Connection.ExecuteAsync(timeout).usp_GetSpokes(minimumSpokes: 6);
            spokes2 = asyncSpokes2.Result;

            if (!spokes2.SequenceEqual(new[] { Spoke.Eight, Spoke.Sixteen }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokes (Enum result) (WithParameter expecting no results) - ");

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

            Console.Write("Calling usp_GetSpokes (Enum result) synchronously (Dynamic Syntax expecting no results) - ");

            spokes2 = db.Database.Connection.Execute(timeout).usp_GetSpokes<Spoke>(minimumSpokes: 32);

            if (!spokes2.Any())
                WriteSuccess();
            else
            {
                WriteError("\t" + spokes2.Count() + " spokes returned");
                return false;
            }

            return true;
        }
    }
}
