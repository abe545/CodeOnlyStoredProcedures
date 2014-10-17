using CodeOnlyStoredProcedure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmokeTests
{
    partial class Program
    {

        static bool DoReturnsOneTests(SmokeDb db)
        {
            Console.Write("Calling usp_ReturnsOne (WithInput) synchronously - ");

            var ro = new ReturnsOne();
            db.ReturnsOne.WithInput(ro).Execute(db.Database.Connection, timeout);
            if (ro.ReturnValue != 1)
            {
                WriteError("\tusp_ReturnsOne did not set the ReturnValue for WithInput");
                return false;
            }

            WriteSuccess();

            Console.Write("Calling usp_ReturnsOne (WithReturnValue) synchronously - ");

            int res = -1;

            db.ReturnsOne.WithReturnValue(i => res = i)
                         .Execute(db.Database.Connection, timeout);
            if (res != 1)
            {
                WriteError("\tusp_ReturnsOne did not set the ReturnValue for WithReturnValue");
                return false;
            }

            WriteSuccess();

            Console.Write("Calling usp_ReturnsOne (Dynamic Syntax) synchronously - ");

            res = -1;
            db.Database.Connection.Execute(timeout).usp_ReturnsOne(returnValue: out res);
            if (res != 1)
            {
                WriteError("\tusp_ReturnsOne did not set the ReturnValue when called dynamically");
                return false;
            }

            WriteSuccess();

            Console.Write("Calling usp_ReturnsOne (WithInput) asynchronously - ");

            ro = new ReturnsOne();
            db.ReturnsOne.WithInput(ro)
                         .ExecuteAsync(db.Database.Connection, timeout)
                         .Wait();
            if (ro.ReturnValue != 1)
            {
                WriteError("\tusp_ReturnsOne did not set the ReturnValue for WithInput");
                return false;
            }

            WriteSuccess();

            Console.Write("Calling usp_ReturnsOne (WithReturnValue) asynchronously - ");

            res = -1;

            db.ReturnsOne.WithReturnValue(i => res = i)
                         .ExecuteAsync(db.Database.Connection, timeout)
                         .Wait();
            if (res != 1)
            {
                WriteError("\tusp_ReturnsOne did not set the ReturnValue for WithReturnValue");
                return false;
            }

            WriteSuccess();

            Console.Write("Calling usp_ReturnsOne (Dynamic Syntax) asynchronously - ");

            ro = new ReturnsOne();
            Task t = db.Database.Connection.ExecuteAsync(timeout).usp_ReturnsOne(ro);
            t.Wait();
            if (ro.ReturnValue != 1)
            {
                WriteError("\tusp_ReturnsOne did not set the ReturnValue when called dynamically");
                return false;
            }

            WriteSuccess();

            Console.Write("Calling usp_ReturnsOne (WithReturnValue) two times asynchronously simultaneously - ");

            var results = new List<int>();
            var sp = db.ReturnsOne.WithReturnValue(i => results.Add(i));

            var t1 = sp.ExecuteAsync(db.Database.Connection, timeout);
            var t2 = sp.ExecuteAsync(db.Database.Connection, timeout);

            Task.WaitAll(t1, t2);

            if (results.Count != 2 || results.Any(i => i != 1))
            {
                var err = new StringBuilder("\tBoth stored procedures did not return 1.");
                for (int i = 0; i < results.Count; ++i)
                    err.AppendFormat("\n\t\tResult {0} - {1}", i, results[i]);

                WriteError(err.ToString());
                return false;
            }

            WriteSuccess();

            return true;
        }

        private class ReturnsOne
        {
            [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
            public int ReturnValue { get; set; }
        }
    }
}
