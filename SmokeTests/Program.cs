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
            var smokeTests      = container.GetExports<Func<IDbConnection,      Tuple<bool, string>>,  ISmokeTest>();
            var asyncSmokeTests = container.GetExports<Func<IDbConnection, Task<Tuple<bool, string>>>, ISmokeTest>();

            var res1 = RunTests(toTest.Database.Connection, smokeTests,      (t, db) => t(db));
            var res2 = RunTests(toTest.Database.Connection, asyncSmokeTests, (t, db) => t(db).Result);

            CodeOnlyStoredProcedure.StoredProcedure.DisableConnectionCloningForEachCall();
            toTest.Database.Connection.Open();

            var res3 = RunTests(toTest.Database.Connection, smokeTests,      (t, db) => t(db),        "No Connection Cloning ");
            var res4 = RunTests(toTest.Database.Connection, asyncSmokeTests, (t, db) => t(db).Result, "No Connection Cloning ");

            var success = res1.Item1 + res2.Item1 + res3.Item1 + res4.Item1;
            var ignore = res1.Item2 + res2.Item2 + res3.Item2 + res4.Item2;
            var fail = res1.Item3 + res2.Item3 + res3.Item3 + res4.Item3;

            if (success > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"{success} smoke tests succeeded!");
            }
            if (ignore > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"{ignore} smoke tests ignored.");
            }
            if (fail > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"{fail} smoke tests failed!");
            }
            
            Exiting();
            return fail > 0 ? 1 : 0;
        }

        private static Tuple<int, int, int> RunTests<T>(
            IDbConnection db,
            IEnumerable<Lazy<T, ISmokeTest>> tests,
            Func<T, IDbConnection, Tuple<bool, string>> runner,
            string prefix = "")
        {
            int success = 0, ignore = 0, fail = 0;
            foreach (var t in tests)
            {
                BeginTest(t.Metadata, prefix);
                if (t.Metadata.Ignore)
                {
                    TestIgnored(t.Metadata, prefix);
                    ++ignore;
                }
                else
                {
                    var res = runner(t.Value, db);
                    if (res.Item1)
                    {
                        TestSucceeded(t.Metadata, prefix);
                        ++success;
                    }
                    else
                    {
                        TestFailed(res.Item2, t.Metadata, prefix);
                        ++fail;
                    }
                }
            }

            return Tuple.Create(success, ignore, fail);
        }

        [Conditional("DEBUG")]
        static void Exiting()
        {
            Console.ReadLine();
        }

        private static void BeginTest(ISmokeTest metadata, string prefix)
        {
            Console.Write($"Running {prefix}{metadata.Name} - ");
            
            if (isInAppveyor)
            {
                var message = BuildAppveyorTestMessage(metadata.Name, AppveyorTestStatus.Running);
                SendAppveyorTestMessage("POST", message);
                testWatch = Stopwatch.StartNew();
            }
        }

        private static void TestSucceeded(ISmokeTest metadata, string prefix)
        {
            if (testWatch != null)
                testWatch.Stop();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Success!");
            Console.ResetColor();

            if (isInAppveyor)
            {
                var message = BuildAppveyorTestMessage(
                    $"{prefix}{metadata.Name}",
                    AppveyorTestStatus.Passed,
                    Tuple.Create("durationMilliseconds", testWatch.ElapsedMilliseconds.ToString()));
                SendAppveyorTestMessage("PUT", message);
            }
        }

        private static void TestIgnored(ISmokeTest metadata, string prefix)
        {
            if (testWatch != null)
                testWatch.Stop();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Ignored.");
            Console.ResetColor();

            if (isInAppveyor)
            {
                var message = BuildAppveyorTestMessage(
                    $"{prefix}{metadata.Name}",
                    AppveyorTestStatus.Ignored,
                    Tuple.Create("durationMilliseconds", testWatch.ElapsedMilliseconds.ToString()));
                SendAppveyorTestMessage("PUT", message);
            }
        }

        private static void TestFailed(string error, ISmokeTest metadata, string prefix)
        {
            if (testWatch != null)
                testWatch.Stop();

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Failed!");
            Console.WriteLine(error);
            Console.ResetColor();
            
            if (isInAppveyor)
            {
                var message = BuildAppveyorTestMessage(
                    $"{prefix}{metadata.Name}",
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
