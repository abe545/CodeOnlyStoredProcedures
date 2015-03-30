using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmokeTests
{
    partial class Program
    {
        public  const  int       timeout = 100;
        private static string    appveyor;
        private static bool      isInAppveyor;
        private static Stopwatch testWatch;
        private static string    assemblyName = Assembly.GetEntryAssembly().GetName().Name;

        static int Main(string[] args)
        {
            appveyor     = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");
            isInAppveyor = !string.IsNullOrEmpty(appveyor);

            var toTest = new SmokeDb();

            var container       = new CompositionContainer(new AssemblyCatalog(Assembly.GetEntryAssembly()));
            var smokeTests      = container.GetExports<Func<IDbConnection,      Tuple<bool, string>>,  IDictionary<string, object>>();
            var asyncSmokeTests = container.GetExports<Func<IDbConnection, Task<Tuple<bool, string>>>, IDictionary<string, object>>();

            var res  = RunTests(toTest.Database.Connection, smokeTests,      (t, db) => t(db));
                res &= RunTests(toTest.Database.Connection, asyncSmokeTests, (t, db) => t(db).Result);

            if (!res)
            {
                // tests failed
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("All tests ran successfully!");
            Exiting();

            return 0;
        }

        private static bool RunTests<T>(
            IDbConnection db,
            IEnumerable<Lazy<T, IDictionary<string, object>>> tests,
            Func<T, IDbConnection, Tuple<bool, string>> runner)
        {
            var result = true;
            foreach (var t in tests)
            {
                BeginTest(t.Metadata);
                var res = runner(t.Value, db);
                result &= res.Item1;
                if (res.Item1)
                    TestSucceeded(t.Metadata);
                else
                    TestFailed(res.Item2, t.Metadata);
            }

            return result;
        }

        [Conditional("DEBUG")]
        static void Exiting()
        {
            Console.ReadLine();
        }

        private static void BeginTest(IDictionary<string, object> metadata)
        {
            var name = metadata["Name"].ToString();
            Console.Write("Running {0} - ", name);
            
            if (isInAppveyor)
            {
                var message = BuildAppveyorTestMessage(name, AppveyorTestStatus.Running);
                SendAppveyorTestMessage("POST", message);
                testWatch = Stopwatch.StartNew();
            }
        }

        private static void TestSucceeded(IDictionary<string, object> metadata)
        {
            if (testWatch != null)
                testWatch.Stop();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Success!");
            Console.ResetColor();

            if (isInAppveyor)
            {
                var name = metadata["Name"].ToString();
                var message = BuildAppveyorTestMessage(
                    name,
                    AppveyorTestStatus.Passed,
                    Tuple.Create("durationMilliseconds", testWatch.ElapsedMilliseconds.ToString()));
                SendAppveyorTestMessage("PUT", message);
            }
        }

        private static void TestFailed(string error, IDictionary<string, object> metadata)
        {
            if (testWatch != null)
                testWatch.Stop();

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Failed!");
            Console.WriteLine(error);
            Console.ResetColor();
            
            if (isInAppveyor)
            {
                var name = metadata["Name"].ToString();
                var message = BuildAppveyorTestMessage(
                    name,
                    AppveyorTestStatus.Failed,
                    Tuple.Create("ErrorMessage", error),
                    Tuple.Create("durationMilliseconds", testWatch.ElapsedMilliseconds.ToString()));
                SendAppveyorTestMessage("PUT", message);
            }
        }

        private static string BuildAppveyorTestMessage(string testName, AppveyorTestStatus status, params Tuple<string, string>[] options)
        {
            var message = new StringBuilder();
            message.AppendLine("{");

            message.AppendFormat("\t\"testName\": \"{0}\",", testName);
            message.AppendLine();
            message.AppendFormat("\t\"fileName\": \"{0}.exe\",", assemblyName);
            message.AppendLine();
            message.AppendFormat("\t\"outcome\": \"{0}\"", status);

            foreach (var t in options)
            {
                message.AppendLine(",");
                message.AppendFormat("\t\"{0}\": \"{1}\"", t.Item1, t.Item2);
            }

            message.AppendLine();
            message.AppendLine("}");

            return message.ToString();
        }

        private static void SendAppveyorTestMessage(string verb, string json)
        {
            using (var wc = new WebClient { BaseAddress = appveyor })
            {
                wc.Headers["Accept"]       = "application/json";
                wc.Headers["Content-type"] = "application/json";

                wc.UploadData("api/tests", verb, Encoding.UTF8.GetBytes(json));
            }
        }

        private enum AppveyorTestStatus
        {
            None,
            Running,
            Passed,
            Failed,
            Ignored,
            Skipped,
            Inconclusive,
            NotFound,
            Cancelled,
            NotRunnable
        }
    }
}
