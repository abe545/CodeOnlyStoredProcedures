using System;
using System.Diagnostics;

namespace SmokeTests
{
    partial class Program
    {
        private const int timeout = 100;

        static int Main(string[] args)
        {
            var toTest = new SmokeDb();

            if (!DoReturnsOneTests(toTest))
            {
                Exiting();
                return -1;
            }

            if (!DoGetItemsTests(toTest))
            {
                Exiting();
                return -1;
            }

            if (!DoGetItemTests(toTest))
            {
                Exiting();
                return -1;
            }

            if (!DoGetWidgetTests(toTest))
            {
                Exiting();
                return -1;
            }

            if (!DoGetSpokesTests(toTest))
            {
                Exiting();
                return -1;
            }

            if (!RunAsyncTests(toTest).Result)
            {
                Exiting();
                return -1;
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("All tests ran successfully!");
            Exiting();

            return 0;
        }

        [Conditional("DEBUG")]
        static void Exiting()
        {
            Console.ReadLine();
        }

        static void WriteSuccess()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Success!");
            Console.ResetColor();
        }

        static void WriteError(string error)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Failed!");
            Console.WriteLine(error);
            Console.ResetColor();
        }
    }
}
