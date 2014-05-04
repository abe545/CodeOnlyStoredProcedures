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
            Console.Write("Calling usp_GetSpokesTests synchronously (No parameters) - ");

            var spokes = db.GetSpokes
                           .Execute(db.Database.Connection);

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokesTests synchronously (WithParameter) - ");

            spokes = db.GetSpokes
                       .WithParameter("minimumSpokes", 4)
                       .Execute(db.Database.Connection);

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokesTests synchronously (WithInput) - ");

            spokes = db.GetSpokes
                       .WithInput(new { minimumSpokes = 6 })
                       .Execute(db.Database.Connection);

            if (!spokes.SequenceEqual(new[] { 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokesTests asynchronously (WithParameter) - ");

            spokes = db.GetSpokes
                       .WithParameter("minimumSpokes", 4)
                       .ExecuteAsync(db.Database.Connection)
                       .Result;

            if (!spokes.SequenceEqual(new[] { 4, 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokesTests asynchronously (WithInput) - ");

            spokes = db.GetSpokes
                       .WithInput(new { minimumSpokes = 6 })
                       .ExecuteAsync(db.Database.Connection)
                       .Result;

            if (!spokes.SequenceEqual(new[] { 8, 16 }))
            {
                WriteError("\treturned the wrong data.");
                return false;
            }
            else
                WriteSuccess();

            Console.Write("Calling usp_GetSpokesTests (WithParameter expecting no results) - ");

            spokes = db.GetSpokes
                       .WithParameter("minimumSpokes", 24)
                       .Execute(db.Database.Connection);

            if (!spokes.Any())
                WriteSuccess();
            else
            {
                WriteError("\t" + spokes.Count() + " spokes returned");
            }

            return true;
        }
    }
}
